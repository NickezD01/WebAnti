using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Subscription;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.Subscription;
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

        /// <summary>
        /// NGHIỆP VỤ MỚI: Đăng ký gói dịch vụ cho TOÀN BỘ CÔNG TY
        /// </summary>
        public async Task<ApiResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // 1. Kiểm tra tài khoản người đại diện thực hiện mua gói (Manager)
                var claim = _claimService.GetUserClaim();
                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (user == null)
                    return apiResponse.SetNotFound("Không tìm thấy thông tin tài khoản người dùng.");

                // Đảm bảo người mua gói phải thuộc về một công ty/doanh nghiệp
                if (user.CompanyId == null)
                    return apiResponse.SetBadRequest("Tài khoản của bạn chưa được liên kết với bất kỳ công ty nào. Không thể mua gói cho tổ chức.");

                int companyId = user.CompanyId.Value;

                // 2. Kiểm tra xem gói dịch vụ định mua có tồn tại/đang kích hoạt không
                var plan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == request.PlanId);
                if (plan == null || !plan.IsActive)
                    return apiResponse.SetNotFound("Gói dịch vụ không tồn tại hoặc đã tạm dừng cung cấp.");

                // 3. Kiểm tra xem Công ty này hiện tại đã có gói nào đang kích hoạt (Active & Paid) hay chưa
                var existingActiveSub = await _unitOfWork.Subscriptions.GetAllAsync(
                    x => x.CompanyId == companyId
                      && x.Status == SubscriptionStatus.Active
                      && x.EndDate > DateTime.UtcNow);

                if (existingActiveSub != null && existingActiveSub.Any())
                {
                    return apiResponse.SetBadRequest("Công ty của bạn hiện đang có một gói dịch vụ đang hoạt động. Vui lòng nâng cấp gói hoặc đợi gói cũ hết hạn.");
                }

                // 4. Tiến hành map và thiết lập thông tin gói cho Công ty
                var subscription = _mapper.Map<Subscription>(request);

                subscription.AccountId = claim.Id; // Người thực hiện giao dịch đại diện
                subscription.CompanyId = companyId; // Công ty sở hữu gói dịch vụ này
                subscription.Price = plan.Price;
                subscription.StartDate = request.StartDate;
                subscription.EndDate = request.StartDate.AddMonths(plan.DurationMonth);
                subscription.NextBillingDate = subscription.EndDate;
                subscription.Status = SubscriptionStatus.Active;
                subscription.PaymentStatus = PaymentStatus.Pending; // Chờ thanh toán hóa đơn
                subscription.UsedSlots = 0; // Reset số lượng nhân viên ban đầu của gói mới

                await _unitOfWork.Subscriptions.AddAsync(subscription);
                await _unitOfWork.SaveChangeAsync();

                // Lấy chi tiết kèm thông tin Plan vừa map để Response hiển thị đúng PlanName
                var detailSub = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(subscription.Id);
                var response = _mapper.Map<SubscriptionResponse>(detailSub ?? subscription);

                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi mua gói dịch vụ cho công ty: {ex.Message}");
            }
        }

        public async Task<ApiResponse> UpdateSubscriptionAsync(int Id, UpdateSubscriptionRequest request)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(Id);
                if (subscription == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy thông tin đăng ký dịch vụ.");

                // SỬA LỖI LOGIC: Kiểm tra đổi PlanId từ request phải check dựa trên request.PlanId chứ không phải Id của Subscription
                if (request.PlanId != subscription.PlanId)
                {
                    var newPlan = await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == request.PlanId);
                    if (newPlan == null || !newPlan.IsActive)
                        return new ApiResponse().SetBadRequest("Gói dịch vụ mới chọn không hợp lệ.");

                    subscription.Price = newPlan.Price;
                    subscription.EndDate = subscription.StartDate.AddMonths(newPlan.DurationMonth);
                    subscription.NextBillingDate = subscription.EndDate;
                }

                _mapper.Map(request, subscription);
                subscription.ModifiedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();

                var response = _mapper.Map<SubscriptionResponse>(subscription);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi cập nhật thông tin đăng ký: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CancelSubscriptionAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(subscriptionId);
                if (subscription == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy thông tin đăng ký.");

                subscription.Status = SubscriptionStatus.Cancelled;
                subscription.ModifiedDate = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();
                return new ApiResponse().SetOk("Đã hủy kích hoạt gói dịch vụ của doanh nghiệp thành công.");
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi hủy gói dịch vụ: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetSubscriptionByIdAsync(int subscriptionId)
        {
            try
            {
                var subscription = await _unitOfWork.Subscriptions.GetSubscriptionWithDetails(subscriptionId);
                if (subscription == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy gói dịch vụ yêu cầu.");

                var response = _mapper.Map<SubscriptionResponse>(subscription);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi dữ liệu: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy lịch sử tất cả các gói dịch vụ mà CÔNG TY đó đã và đang sử dụng
        /// </summary>
        public async Task<ApiResponse> GetUserSubscriptionsAsync(int accountId)
        {
            try
            {
                // Lấy thông tin công ty của tài khoản đang request
                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == accountId);
                if (user?.CompanyId == null)
                    return new ApiResponse().SetOk(new List<SubscriptionResponse>());

                // Lấy toàn bộ các đăng ký của Công ty (thay vì lọc theo cá nhân AccountId)
                var subscriptions = await _unitOfWork.Subscriptions.GetAllAsync(
                    x => x.CompanyId == user.CompanyId.Value,
                    include: q => q.Include(s => s.SubscriptionPlans).Include(s => s.Account));

                var response = _mapper.Map<List<SubscriptionResponse>>(subscriptions);
                return new ApiResponse().SetOk(response);
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi lấy danh sách gói của doanh nghiệp: {ex.Message}");
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
                    subscription.ModifiedDate = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangeAsync();
                return new ApiResponse().SetOk($"Đã xử lý hoàn tất {expiringSubscriptionss.Count} gói dịch vụ hết hạn.");
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi xử lý tự động quét hết hạn: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CheckSubscriptionStatusAsync(int accountId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == accountId);
                if (user?.CompanyId == null)
                {
                    return new ApiResponse().SetOk(new { HasActiveSubscription = false, ActiveSubscription = (object)null });
                }

                // Kiểm tra gói kích hoạt theo CompanyId
                var activeSubscription = await GetActiveSubscriptionByCompany(user.CompanyId.Value, accountId);
                if (activeSubscription != null)
                {
                    var response = _mapper.Map<SubscriptionResponse>(activeSubscription);
                    return new ApiResponse().SetOk(new
                    {
                        HasActiveSubscription = true,
                        ActiveSubscription = response
                    });
                }

                return new ApiResponse().SetOk(new { HasActiveSubscription = false, ActiveSubscription = (object)null });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi kiểm tra trạng thái gói: {ex.Message}");
            }
        }

        public async Task<ApiResponse> DeleteSubPlanData(int Id)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                var plan = await _unitOfWork.SubscriptionPlans.GetAsync(c => c.Id == Id);
                if (plan == null)
                    return apiResponse.SetNotFound("Không tìm thấy cấu hình gói dịch vụ mẫu.");

                await _unitOfWork.SubscriptionPlans.RemoveByIdAsync(Id);
                await _unitOfWork.SaveChangeAsync();
                return apiResponse.SetOk("Xóa gói dịch vụ mẫu thành công!");
            }
            catch (Exception e)
            {
                return apiResponse.SetBadRequest(e.Message);
            }
        }

        // =====================================================================
        // PHẦN QUOTA & QUẢN LÝ NHÂN VIÊN
        // =====================================================================

        public async Task<ApiResponse> InviteEmployeeAsync(InviteEmployeeRequest request, int managerId)
        {
            try
            {
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy thông tin Manager.");
                if (manager.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Manager chưa được gán vào công ty.");

                int companyId = manager.CompanyId.Value;

                var activeSub = await GetActiveSubscriptionByCompany(companyId, managerId);
                if (activeSub == null)
                    return new ApiResponse().SetBadRequest("Công ty chưa đăng ký hoặc chưa kích hoạt thành công gói dịch vụ.");

                var plan = activeSub.SubscriptionPlans
                    ?? await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == activeSub.PlanId);
                if (plan == null)
                    return new ApiResponse().SetBadRequest("Không tìm thấy thông tin cấu hình slots của gói.");

                if (activeSub.UsedSlots >= plan.MaxSlots)
                    return new ApiResponse().SetBadRequest($"Gói '{plan.Name}' của công ty đã dùng hết {plan.MaxSlots} slot. Vui lòng nâng cấp gói thêm nhân viên.");

                var existingUser = await _unitOfWork.Users.GetAsync(x => x.Email == request.Email);
                string tempPassword = GenerateTempPassword();

                if (existingUser != null)
                {
                    if (existingUser.CompanyId == companyId)
                        return new ApiResponse().SetBadRequest("Nhân viên này đã có trong tổ chức doanh nghiệp của bạn.");

                    existingUser.CompanyId = companyId;
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    activeSub.UsedSlots++;
                    activeSub.ModifiedDate = DateTime.UtcNow;
                    await _unitOfWork.SaveChangeAsync();

                    await _emailService.SendNotiMail(existingUser.Email,
                        $"Xin chào <b>{existingUser.FullName}</b>,<br/><br/>" +
                        $"Tổ chức của bạn vừa kích hoạt liên kết thành viên trên hệ thống <b>AntiPhisher</b>.<br/>" +
                        $"Hãy đăng nhập để tham gia các chiến dịch giả lập của công ty.");

                    return new ApiResponse().SetOk(new
                    {
                        Message = "Đã liên kết tài khoản nhân viên vào công ty thành công.",
                        UserId = existingUser.UserId,
                        Email = existingUser.Email,
                        UsedSlots = activeSub.UsedSlots,
                        RemainingSlots = plan.MaxSlots - activeSub.UsedSlots
                    });
                }

                var pwd = CreatePasswordHash(tempPassword);
                var newEmployee = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    PasswordHash = Convert.ToBase64String(pwd.Hash),
                    PasswordSalt = Convert.ToBase64String(pwd.Salt),
                    RoleId = 3, // Nhân viên (Employee)
                    CompanyId = companyId,
                    IsActive = true,
                    IsEmailVerified = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(newEmployee);

                activeSub.UsedSlots++;
                activeSub.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.SaveChangeAsync();

                var mailResult = await _emailService.SendNotiMail(request.Email,
                    $"Xin chào <b>{request.FullName}</b>,<br/><br/>" +
                    $"Bạn được cấp tài khoản đào tạo An toàn thông tin doanh nghiệp tại <b>AntiPhisher</b>.<br/>" +
                    $"• Email: <b>{request.Email}</b><br/>" +
                    $"• Mật khẩu tạm thời: <b>{tempPassword}</b><br/><br/>" +
                    $"Vui lòng thực hiện đổi mật khẩu ngay trong phiên đăng nhập đầu tiên.");


                if (!mailResult.IsSuccess)
                {
                    Console.Error.WriteLine($"[INVITE WARNING] SendNotiMail fail: {mailResult.ErrorMessage}");
                }

                return new ApiResponse().SetOk(new
                {
                    Message = "Tạo và gửi mail mời nhân viên thành công.",
                    UserId = newEmployee.UserId,
                    Email = newEmployee.Email,
                    UsedSlots = activeSub.UsedSlots,
                    RemainingSlots = plan.MaxSlots - activeSub.UsedSlots
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi mời nhân viên: {ex.Message}");
            }
        }

        public async Task<ApiResponse> GetSlotsUsageAsync(int managerId)
        {
            try
            {
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager?.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Manager chưa được cấu hình định danh công ty.");

                var activeSub = await GetActiveSubscriptionByCompany(manager.CompanyId.Value, managerId);
                if (activeSub == null)
                    return new ApiResponse().SetOk(new SlotsUsageResponse
                    {
                        UsedSlots = 0,
                        TotalSlots = 0,
                        RemainingSlots = 0,
                        PlanName = "Chưa đăng ký gói tổ chức"
                    });

                var plan = activeSub.SubscriptionPlans
                    ?? await _unitOfWork.SubscriptionPlans.GetAsync(p => p.Id == activeSub.PlanId);

                int maxSlots = plan?.MaxSlots ?? 0;
                string planName = plan?.Name ?? "Không rõ tên gói";

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
                return new ApiResponse().SetBadRequest($"Lỗi lấy hạn mức vị trí: {ex.Message}");
            }
        }

        public async Task<ApiResponse> RemoveEmployeeAsync(int employeeUserId, int managerId)
        {
            try
            {
                var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == managerId);
                if (manager?.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Tài khoản quản lý không hợp lệ.");

                int companyId = manager.CompanyId.Value;

                var employee = await _unitOfWork.Users.GetAsync(x => x.UserId == employeeUserId);
                if (employee == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy nhân viên.");
                if (employee.CompanyId != companyId)
                    return new ApiResponse().SetBadRequest("Nhân viên không thuộc quyền quản lý của tổ chức bạn.");
                if (employee.UserId == managerId)
                    return new ApiResponse().SetBadRequest("Hệ thống bảo mật không cho phép tự loại bỏ tài khoản chính mình.");

                employee.CompanyId = null;
                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;

                var activeSub = await GetActiveSubscriptionByCompany(companyId, managerId);
                if (activeSub != null && activeSub.UsedSlots > 0)
                {
                    activeSub.UsedSlots--;
                    activeSub.ModifiedDate = DateTime.UtcNow;
                }

                await _unitOfWork.SaveChangeAsync();

                return new ApiResponse().SetOk(new
                {
                    Message = $"Đã loại bỏ nhân viên '{employee.FullName}' và hoàn trả 1 slot vào bộ nhớ đệm.",
                    RemainingSlots = activeSub != null ? (activeSub.SubscriptionPlans?.MaxSlots ?? 0) - activeSub.UsedSlots : 0
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi xử lý xóa nhân viên: {ex.Message}");
            }
        }

        private async Task<Subscription?> GetActiveSubscriptionByCompany(int companyId, int managerId)
        {
            var byCompany = await _unitOfWork.Subscriptions.GetAllAsync(
                x => x.CompanyId == companyId
                  && x.Status == SubscriptionStatus.Active
                  && x.EndDate > DateTime.UtcNow,
                include: q => q.Include(s => s.SubscriptionPlans));

            if (byCompany?.Any() == true)
                return byCompany.OrderByDescending(s => s.EndDate).First();

            var byAccount = await _unitOfWork.Subscriptions.GetActiveSubscriptionsByAccountId(managerId);
            return byAccount
                .Where(s => s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefault();
        }

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