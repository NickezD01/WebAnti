using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.SubscriptionPlan;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.SubscriptionPlan;
using AntiPhisher.Domain.Models;
using AutoMapper;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IClaimService _claimService;
        private readonly IValidator<CreateSubscriptionPlanRequest> _subplanValidator;

        public SubscriptionPlanService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IClaimService claimService,
            IValidator<CreateSubscriptionPlanRequest> subplanValidator
            )
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _claimService = claimService;
            _subplanValidator = subplanValidator;
        }

        public async Task<ApiResponse> CreatePlanAsync(CreateSubscriptionPlanRequest request)
        {
            ApiResponse apiResponse = new ApiResponse();
            var validationResult = _subplanValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return apiResponse.SetBadRequest(string.Join(", ", errors));
            }
            try
            {
                var userClaim = _claimService.GetUserClaim();
                if (userClaim.Role != "Manager")
                {
                    return apiResponse.SetBadRequest("Only managers can create subscription plans");
                }

                //if (await _unitOfWork.SubscriptionPlans.IsPlanNameExists(request.Name))
                //{
                //    return apiResponse.SetBadRequest("A plan with this name already exists");
                //}

                var plan = _mapper.Map<SubscriptionPlan>(request);

                // Đảm bảo isActive luôn là false khi tạo mới - ghi đè sau khi mapping
                plan.IsActive = true;

                await _unitOfWork.SubscriptionPlans.AddAsync(plan);
                await _unitOfWork.SaveChangeAsync();

                // Đảm bảo response cũng có isActive = false
                var response = _mapper.Map<SubscriptionPlanResponse>(plan);
                response.IsActive = true; // Đảm bảo response cũng có giá trị đúng

                return apiResponse.SetOk(response);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Error creating subscription plan: {ex.Message}");
            }
        }


        public async Task<ApiResponse> UpdatePlanAsync(int Id, UpdateSubscriptionPlanRequest request)
        {
            try
            {
                var userClaim = _claimService.GetUserClaim();
                if (userClaim.Role != "Manager")
                {
                    return new ApiResponse().SetBadRequest("Only managers can update subscription plans");
                }

                var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == Id);
                if (plan == null)
                {
                    return new ApiResponse().SetNotFound("Subscription plan not found");
                }
                _mapper.Map(request, plan);
                await _unitOfWork.SaveChangeAsync();

                var response = _mapper.Map<SubscriptionPlanResponse>(plan);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error updating subscription plan: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeletePlanAsync(int planId)
        {
            try
            {
                // Verify if user is Manager
                var userClaim = _claimService.GetUserClaim();
                if (userClaim.Role != "Manager")
                {
                    return new ApiResponse().SetBadRequest("Only managers can delete subscription plans");
                }
                var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == planId);
                if (plan == null)
                {
                    return new ApiResponse().SetNotFound("Subscription plan not found");
                }

                // Check if plan has active subscribers
                //var activeSubscribers = await _unitOfWork.SubscriptionPlans.GetTotalSubscribersCount(planId);
                //if (activeSubscribers > 0)
                //{
                //    return new ApiResponse().SetBadRequest("Cannot delete plan with active subscribers");
                //}

                plan.IsDeleted = true;
                plan.ModifiedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();
                return new ApiResponse().SetOk("Subscription plan deleted successfully");
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error deleting subscription plan: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetPlanByIdAsync(int planId)
        {
            try
            {
                // Lấy plan với thông tin về subscribers
                var plan = await _unitOfWork.SubscriptionPlans.GetPlanWithSubscribers(planId);

                if (plan == null || plan.IsDeleted)
                {
                    return new ApiResponse().SetNotFound("Subscription plan not found");
                }

                // Tính tổng doanh thu
                decimal totalRevenue = 0;
                if (plan.Subscriptions != null && plan.Subscriptions.Any())
                {
                    totalRevenue = plan.Subscriptions
                        .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                        .Sum(s => s.Price);
                }

                // Tạo anonymous object mới chỉ chứa các trường cần thiết
                var filteredResponse = new
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Price = plan.Price,
                    ActiveSubscribersCount = plan.Subscriptions?.Count(s => s.Status == SubscriptionStatus.Active) ?? 0,

                    TotalRevenue = totalRevenue
                };

                return new ApiResponse().SetOk(filteredResponse);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error retrieving subscription plan: {ex.Message}");
            }
        }



        public async Task<ApiResponse> GetAllPlansAsync()
        {
            try
            {
                var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync(p => !p.IsDeleted);

                // Đảm bảo include Subscriptions để có thể đếm số người đăng ký
                var plansWithSubscribers = new List<SubscriptionPlan>();
                foreach (var plan in plans)
                {
                    var planWithSubs = await _unitOfWork.SubscriptionPlans.GetPlanWithSubscribers(plan.Id);
                    if (planWithSubs != null)
                    {
                        plansWithSubscribers.Add(planWithSubs);
                    }
                }

                var response = _mapper.Map<List<SubscriptionPlanResponse>>(plansWithSubscribers);

                // Không tính toán hoặc gán TotalRevenue nữa

                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error retrieving subscription plans: {ex.Message}");
            }
        }


        //admindashboard
        //admindashboard
        public async Task<ApiResponse> CountPlan()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync(null);
            var subs = await _unitOfWork.Subscriptions.GetAllAsync(s => s.PaymentStatus == PaymentStatus.Paid);
            var counts = new Dictionary<SubscriptionPlanName, int>();
            int totalBronzeCount = 0;
            int totalSilverCount = 0;
            int totalGoldCount = 0;
            foreach (var plan in plans)
            {
                if (plan.Name == SubscriptionPlanName.Bronze)
                {
                    totalBronzeCount += subs.Count(sub => sub.PlanId == plan.Id);
                }
                else if (plan.Name == SubscriptionPlanName.Silver)
                {
                    totalSilverCount += subs.Count(sub => sub.PlanId == plan.Id);
                }
                else if (plan.Name == SubscriptionPlanName.Gold)
                {
                    totalGoldCount += subs.Count(sub => sub.PlanId == plan.Id);
                }
            }
            counts[SubscriptionPlanName.Bronze] = totalBronzeCount;
            counts[SubscriptionPlanName.Silver] = totalSilverCount;
            counts[SubscriptionPlanName.Gold] = totalGoldCount;
            return new ApiResponse().SetOk(counts);
        }
        public async Task<ApiResponse> CalculateTotalRevenue()
        {
            var Bronzeplans = await _unitOfWork.SubscriptionPlans.GetAllAsync(b => b.Name == SubscriptionPlanName.Bronze);
            var Silverplans = await _unitOfWork.SubscriptionPlans.GetAllAsync(b => b.Name == SubscriptionPlanName.Silver);
            var Goldplans = await _unitOfWork.SubscriptionPlans.GetAllAsync(b => b.Name == SubscriptionPlanName.Gold);
            var subs = await _unitOfWork.Subscriptions.GetAllAsync(s => s.PaymentStatus == PaymentStatus.Paid);


            var totalRevenue = new Dictionary<SubscriptionPlanName, decimal>();
            decimal totalBronzePrize = 0;
            decimal totalSilverPrize = 0;
            decimal totalGoldPrize = 0;

            foreach (var bplan in Bronzeplans)
            {
                int userCount = subs.Count(sub => sub.PlanId == bplan.Id);
                totalBronzePrize += userCount * bplan.Price;
            }
            foreach (var splan in Silverplans)
            {
                int userCount = subs.Count(sub => sub.PlanId == splan.Id);
                totalSilverPrize += userCount * splan.Price;
            }
            foreach (var gplan in Goldplans)
            {
                int userCount = subs.Count(sub => sub.PlanId == gplan.Id);
                totalGoldPrize += userCount * gplan.Price;
            }
            totalRevenue[SubscriptionPlanName.Bronze] = totalBronzePrize;
            totalRevenue[SubscriptionPlanName.Silver] = totalSilverPrize;
            totalRevenue[SubscriptionPlanName.Gold] = totalGoldPrize;

            return new ApiResponse().SetOk(totalRevenue);
        }
        public async Task<ApiResponse> TotalPrice()
        {
            var planBronze = await _unitOfWork.SubscriptionPlans.GetAsync(b => b.Name == SubscriptionPlanName.Bronze);
            var subs = await _unitOfWork.Subscriptions.GetAllAsync(s => s.PaymentStatus == PaymentStatus.Paid);
            int bronzeUser = 0;
            if (subs != null)
            {
                foreach (var sub in subs)
                {
                    if (sub.PlanId == planBronze.Id)
                    {
                        bronzeUser++;
                    }
                }
            }
            else
            {
                bronzeUser = 0;
            }


            var planSilver = await _unitOfWork.SubscriptionPlans.GetAsync(b => b.Name == SubscriptionPlanName.Silver);
            int silverUser = 0;
            if (subs != null)
            {
                foreach (var sub in subs)
                {
                    if (sub.PlanId == planSilver.Id)
                    {
                        silverUser++;
                    }
                }
            }
            else
            {
                silverUser = 0;
            }


            var planGold = await _unitOfWork.SubscriptionPlans.GetAsync(b => b.Name == SubscriptionPlanName.Gold);
            int goldUser = 0;
            if (subs != null)
            {
                foreach (var sub in subs)
                {
                    if (sub.PlanId == planGold.Id)
                    {
                        goldUser++;
                    }
                }
            }
            else
            {
                goldUser = 0;
            }

            var totalprice = (bronzeUser * planBronze.Price) + (silverUser * planSilver.Price) + (goldUser * planGold.Price);
            return new ApiResponse().SetOk(totalprice);
        }


    }
}
