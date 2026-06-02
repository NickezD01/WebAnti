using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Subscription;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.Subscription;
using AntiPhisher.Application.Services;
using AntiPhisher.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IClaimService _claimService;
        private readonly IEmailService _emailService;

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IClaimService claimService,
            IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _claimService = claimService;
            _emailService = emailService;
        }

        public async Task<ApiResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();

                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (user == null)
                    return apiResponse.SetNotFound("User not found");

                var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == request.PlanId);
                if (plan == null || !plan.IsActive)
                    return apiResponse.SetNotFound("Subscription plan not found or inactive");

                var subscription = _mapper.Map<Subscription>(request);
                subscription.AccountId = claim.Id;
                subscription.Price = plan.Price;
                subscription.EndDate = request.StartDate.AddMonths(plan.DurationMonth);
                subscription.NextBillingDate = subscription.EndDate;

                // NEW: Liên kết CompanyId của Manager vào Subscription ngay khi tạo
                subscription.CompanyId = user.CompanyId;
                subscription.UsedSlots = 0;

                await _unitOfWork.Subscriptions.AddAsync(subscription);
                await _unitOfWork.SaveChangeAsync();

                var response = _mapper.Map<SubscriptionResponse>(subscription);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error creating subscription: {ex.Message}");
            }
        }


        public async Task<ApiResponse> UpdateSubscriptionAsync(int Id, UpdateSubscriptionRequest request)
        {

            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(Id);
                if (subscription == null)
                    return new ApiResponse().SetNotFound("Subscription not found");

                if (request.PlanId != subscription.PlanId)
                {
                    var newPlan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == Id);
                    if (newPlan == null || !newPlan.IsActive)
                        return new ApiResponse().SetBadRequest("Invalid subscription plan");
                }

                _mapper.Map(request, subscription);
                await _unitOfWork.SaveChangeAsync();

                var response = _mapper.Map<SubscriptionResponse>(subscription);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error updating subscription: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CancelSubscriptionAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(subscriptionId);
                if (subscription == null)
                    return new ApiResponse().SetNotFound("Subscription not found");

                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.ModifiedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();
                return new ApiResponse().SetOk("Subscription cancelled successfully");
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error cancelling subscription: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetSubscriptionByIdAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(subscriptionId);
                if (subscription == null)
                    return new ApiResponse().SetNotFound("Subscription not found");

                var response = _mapper.Map<SubscriptionResponse>(subscription);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error retrieving subscription: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetUserSubscriptionsAsync(int accountId)
        {
            try
            {
                var subscriptions = await _unitOfWork.Subscriptions.GetSubscriptionHistory(accountId);
                var response = _mapper.Map<List<SubscriptionResponse>>(subscriptions);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error retrieving user subscriptions: {ex.Message}");
            }
        }


        public Task<ApiResponse> UpgradeSubscriptionPlanAsync(int subscriptionId, int newPlanId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse> ProcessSubscriptionPaymentAsync(int subscriptionId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse> HandleExpiredSubscriptionsAsync()
        {
            try
            {
                var expiringSubscriptionss = await _unitOfWork.Subscriptions
                    .GetExpiringSubscriptions(DateTime.UtcNow);

                foreach (var subscription in expiringSubscriptionss)
                {
                    subscription.Status = SubscriptionStatus.Expired;
                }

                await _unitOfWork.SaveChangeAsync();
                return new ApiResponse().SetOk($"Processed {expiringSubscriptionss.Count} expired subscriptions");
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error handling expired subscriptions: {ex.Message}");
            }
        }



        public async Task<ApiResponse> CheckSubscriptionStatusAsync(int accountId)
        {
            try
            {
                var hasActiveSubscription = await _unitOfWork.Subscriptions.HasActiveSubscription(accountId);

                if (hasActiveSubscription)
                {
                    // Lấy subscription active mới nhất
                    var activeSubscriptions = await _unitOfWork.Subscriptions.GetActiveSubscriptionsByAccountId(accountId);
                    if (activeSubscriptions.Any())
                    {
                        var activeSubscription = activeSubscriptions.OrderByDescending(s => s.EndDate).First();
                        var response = _mapper.Map<SubscriptionResponse>(activeSubscription);
                        return new ApiResponse().SetOk(new
                        {
                            HasActiveSubscription = true,
                            ActiveSubscription = response
                        });
                    }
                }

                return new ApiResponse().SetOk(new
                {
                    HasActiveSubscription = false,
                    ActiveSubscription = (object)null
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Error checking subscription status: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteSubPlanData(int Id)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var children = await _unitOfWork.SubscriptionPlans.GetAsync(c => c.Id == Id);
                if (children == null)
                {
                    return apiResponse.SetNotFound("Can not found the Children detail");
                }
                await _unitOfWork.SubscriptionPlans.RemoveByIdAsync(Id);
                await _unitOfWork.SaveChangeAsync();
                return apiResponse.SetOk("Deleled successfully!");
            }
            catch (Exception e)
            {
                return apiResponse.SetBadRequest(e.Message);
            }
        }

        // =====================================================================
        // PHẦN 1 — Quota & Quản lý nhân viên
        // =====================================================================

        public async Task<ApiResponse> InviteEmployeeAsync(InviteEmployeeRequest request, int managerId)
        {
            try
            {
                // 1. Lấy Manager + CompanyId
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy thông tin Manager.");
                if (manager.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty. Vui lòng liên hệ Admin.");

                int companyId = manager.CompanyId.Value;

                // 2. Lấy Subscription đang active — NOW: dùng CompanyId thay vì AccountId
                var activeSub = await GetActiveSubscriptionByCompany(companyId, managerId);
                if (activeSub == null)
                    return new ApiResponse().SetBadRequest("Công ty chưa có gói dịch vụ đang hoạt động. Vui lòng mua gói trước.");

                // 3. Lấy Plan để biết MaxSlots
                var plan = activeSub.SubscriptionPlans
                    ?? await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == activeSub.PlanId);
                if (plan == null)
                    return new ApiResponse().SetBadRequest("Không tìm thấy thông tin gói dịch vụ.");

                // 4. Kiểm tra quota bằng UsedSlots — không cần COUNT(*) nữa
                if (activeSub.UsedSlots >= plan.MaxSlots)
                    return new ApiResponse().SetBadRequest(
                        $"Gói '{plan.Name}' đã dùng hết {plan.MaxSlots} slot. Vui lòng nâng cấp gói để mời thêm nhân viên.");

                // 5. Kiểm tra email đã tồn tại
                var existingUser = await _unitOfWork.Users.GetAsync(x => x.Email == request.Email);
                string tempPassword = GenerateTempPassword();

                if (existingUser != null)
                {
                    if (existingUser.CompanyId == companyId)
                        return new ApiResponse().SetBadRequest("Nhân viên này đã là thành viên trong công ty của bạn.");

                    existingUser.CompanyId = companyId;
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    // Tăng UsedSlots trên Subscription
                    activeSub.UsedSlots++;
                    activeSub.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.SaveChangeAsync();

                    await _emailService.SendNotiMail(existingUser.Email,
                        $"Xin chào <b>{existingUser.FullName}</b>,<br/><br/>" +
                        $"Bạn đã được thêm vào tổ chức trên hệ thống <b>AntiPhisher</b>.<br/>" +
                        $"Hãy đăng nhập bằng tài khoản hiện có để tiếp tục.");

                    return new ApiResponse().SetOk(new
                    {
                        Message = "Đã liên kết tài khoản hiện có vào công ty thành công.",
                        UserId = existingUser.UserId,
                        Email = existingUser.Email,
                        UsedSlots = activeSub.UsedSlots,
                        RemainingSlots = plan.MaxSlots - activeSub.UsedSlots
                    });
                }

                // 6. Tạo tài khoản mới
                var pwd = CreatePasswordHash(tempPassword);
                var newEmployee = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = Convert.ToBase64String(pwd.Hash),
                    PasswordSalt = Convert.ToBase64String(pwd.Salt),
                    RoleId = 3,
                    CompanyId = companyId,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(newEmployee);

                // Tăng UsedSlots trên Subscription
                activeSub.UsedSlots++;
                activeSub.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.SaveChangeAsync();

                await _emailService.SendNotiMail(request.Email,
                    $"Xin chào <b>{request.FullName}</b>,<br/><br/>" +
                    $"Bạn đã được mời tham gia hệ thống đào tạo <b>AntiPhisher</b>.<br/>" +
                    $"• Email: <b>{request.Email}</b><br/>" +
                    $"• Mật khẩu tạm thời: <b>{tempPassword}</b><br/><br/>" +
                    $"Vui lòng đổi mật khẩu sau lần đăng nhập đầu tiên.");

                return new ApiResponse().SetOk(new
                {
                    Message = "Mời nhân viên thành công.",
                    UserId = newEmployee.UserId,
                    Email = newEmployee.Email,
                    UsedSlots = activeSub.UsedSlots,
                    RemainingSlots = plan.MaxSlots - activeSub.UsedSlots
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi mời nhân viên: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetSlotsUsageAsync(int managerId)
        {
            try
            {
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager?.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty.");

                // Dùng UsedSlots trực tiếp — không COUNT(*) nữa
                var activeSub = await GetActiveSubscriptionByCompany(manager.CompanyId.Value, managerId);

                if (activeSub == null)
                    return new ApiResponse().SetOk(new SlotsUsageResponse
                    {
                        UsedSlots = 0,
                        TotalSlots = 0,
                        RemainingSlots = 0,
                        PlanName = "Chưa có gói dịch vụ"
                    });

                var plan = activeSub.SubscriptionPlans
                    ?? await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == activeSub.PlanId);

                int maxSlots = plan?.MaxSlots ?? 0;
                string planName = plan?.Name ?? "Không xác định";

                return new ApiResponse().SetOk(new SlotsUsageResponse
                {
                    UsedSlots = activeSub.UsedSlots,
                    TotalSlots = maxSlots,
                    RemainingSlots = Math.Max(0, maxSlots - activeSub.UsedSlots),
                    PlanName = planName
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi lấy thông tin slot: {ex.Message}");
            }
        }

        public async Task<ApiResponse> RemoveEmployeeAsync(int employeeUserId, int managerId)
        {
            try
            {
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager?.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty.");

                int companyId = manager.CompanyId.Value;

                var employee = await _unitOfWork.Users.GetAsync(x => x.UserId == employeeUserId);
                if (employee == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy nhân viên.");
                if (employee.CompanyId != companyId)
                    return new ApiResponse().SetBadRequest("Nhân viên này không thuộc công ty của bạn.");
                if (employee.UserId == managerId)
                    return new ApiResponse().SetBadRequest("Không thể xóa chính mình.");

                // Gỡ liên kết công ty
                employee.CompanyId = null;
                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;

                // Giảm UsedSlots trên Subscription (tránh về âm)
                var activeSub = await GetActiveSubscriptionByCompany(companyId, managerId);
                if (activeSub != null && activeSub.UsedSlots > 0)
                {
                    activeSub.UsedSlots--;
                    activeSub.ModifiedDate = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangeAsync();

                return new ApiResponse().SetOk(new
                {
                    Message = $"Đã xóa nhân viên '{employee.FullName}' khỏi công ty và giải phóng 1 slot.",
                    RemainingSlots = activeSub != null
                        ? (activeSub.SubscriptionPlans?.MaxSlots ?? 0) - activeSub.UsedSlots
                        : 0
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi xóa nhân viên: {ex.Message}");
            }
        }

        // =====================================================================
        // Helper: lấy Subscription active của Company/Manager
        // Ưu tiên CompanyId, fallback theo AccountId
        // =====================================================================
        private async Task<Subscription?> GetActiveSubscriptionByCompany(int companyId, int managerId)
        {
            // Tìm sub active theo CompanyId (subscription mới có trường này)
            var byCompany = await _unitOfWork.Subscriptions.GetAllAsync(
                x => x.CompanyId == companyId
                  && x.Status == SubscriptionStatus.Active
                  && x.PaymentStatus == PaymentStatus.Paid
                  && x.EndDate > DateTime.UtcNow,
                include: q => q.Include(s => s.SubscriptionPlans));

            if (byCompany?.Any() == true)
                return byCompany.OrderByDescending(s => s.EndDate).First();

            // Fallback: subscription cũ chưa có CompanyId → tìm theo AccountId
            var byAccount = await _unitOfWork.Subscriptions.GetActiveSubscriptionsByAccountId(managerId);
            return byAccount
                .Where(s => s.Status == SubscriptionStatus.Active && s.PaymentStatus == PaymentStatus.Paid)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefault();
        }

        // =====================================================================
        // Helper: sinh mật khẩu tạm thời + hash
        // =====================================================================

        private static string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static (byte[] Hash, byte[] Salt) CreatePasswordHash(string password)
        {
            using var hmac = new HMACSHA512();
            return (hmac.ComputeHash(Encoding.UTF8.GetBytes(password)), hmac.Key);
        }
    }
}
