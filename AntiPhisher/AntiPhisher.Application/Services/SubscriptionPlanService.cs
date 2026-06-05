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
            if (request == null) return apiResponse.SetBadRequest("Dữ liệu request không được để trống.");
            var validationResult = _subplanValidator.Validate(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return apiResponse.SetBadRequest(string.Join(", ", errors));
            }
            try
            {
                var userClaim = _claimService.GetUserClaim();
                if (userClaim.Role != "Admin")
                {
                    return apiResponse.SetBadRequest("Only admins can create subscription plans");
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
                if (userClaim.Role != "Admin")
                {
                    return new ApiResponse().SetBadRequest("Only admins can update subscription plans");
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
                var userClaim = _claimService.GetUserClaim();
                if (userClaim.Role != "Admin")
                {
                    return new ApiResponse().SetBadRequest("Only admins can delete subscription plans");
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

        // Public — chỉ trả field bán hàng, không lộ dữ liệu nội bộ
        public async Task<ApiResponse> GetPlanByIdAsync(int planId)
        {
            try
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == planId && !p.IsDeleted);
                if (plan == null)
                    return new ApiResponse().SetNotFound("Subscription plan not found");

                var response = new
                {
                    plan.Id,
                    plan.Name,
                    plan.Price,
                    plan.Description,
                    plan.Feature,
                    plan.MaxSlots,
                    DurationInMonths = plan.DurationMonth,
                    plan.IsActive
                };

                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error retrieving subscription plan: {ex.Message}");
            }
        }

        // Admin-only — trả thêm ActiveSubscribersCount và TotalRevenue
        public async Task<ApiResponse> GetPlanStatsAsync(int planId)
        {
            try
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetPlanWithSubscribers(planId);
                if (plan == null || plan.IsDeleted)
                    return new ApiResponse().SetNotFound("Subscription plan not found");

                decimal totalRevenue = plan.Subscriptions?
                    .Where(s => s.PaymentStatus == PaymentStatus.Paid)
                    .Sum(s => s.Price) ?? 0;

                var response = new
                {
                    plan.Id,
                    plan.Name,
                    plan.Price,
                    plan.Description,
                    plan.Feature,
                    plan.MaxSlots,
                    DurationInMonths = plan.DurationMonth,
                    plan.IsActive,
                    ActiveSubscribersCount = plan.Subscriptions?.Count(s => s.Status == SubscriptionStatus.Active) ?? 0,
                    TotalRevenue = totalRevenue
                };

                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error retrieving subscription plan stats: {ex.Message}");
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


        // =====================================================================
        // Admin Dashboard: Đếm số subscriber theo từng gói
        // CHANGED: SubscriptionPlanName enum → string comparison
        // =====================================================================
        public async Task<ApiResponse> CountPlan()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync(null);
            var subs  = await _unitOfWork.Subscriptions.GetAllAsync(s => s.PaymentStatus == PaymentStatus.Paid);

            // Group động theo tên gói (không lock cứng Bronze/Silver/Gold nữa)
            var counts = plans.ToDictionary(
                plan => plan.Name,
                plan => subs.Count(sub => sub.PlanId == plan.Id)
            );

            return new ApiResponse().SetOk(counts);
        }

        public async Task<ApiResponse> CalculateTotalRevenue()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync(null);
            var subs  = await _unitOfWork.Subscriptions.GetAllAsync(s => s.PaymentStatus == PaymentStatus.Paid);

            // Tính doanh thu động theo từng gói
            var totalRevenue = plans.ToDictionary(
                plan => plan.Name,
                plan =>
                {
                    int userCount = subs.Count(sub => sub.PlanId == plan.Id);
                    return userCount * plan.Price;
                }
            );

            return new ApiResponse().SetOk(totalRevenue);
        }

        public async Task<ApiResponse> TotalPrice()
        {
            var plans = await _unitOfWork.SubscriptionPlans.GetAllAsync(null);
            var subs  = await _unitOfWork.Subscriptions.GetAllAsync(s => s.PaymentStatus == PaymentStatus.Paid);

            decimal total = 0;
            foreach (var plan in plans)
            {
                int count = subs.Count(sub => sub.PlanId == plan.Id);
                total += count * plan.Price;
            }

            return new ApiResponse().SetOk(total);
        }


    }
}
