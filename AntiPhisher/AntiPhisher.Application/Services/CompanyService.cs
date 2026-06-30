using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CompanyRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.CompanyResponse;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClaimService _claimService;
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        // Địa chỉ gốc FE — dùng để tạo link xác nhận trong email
        private const string FE_BASE_URL = "http://localhost:5173";

        public CompanyService(
            IUnitOfWork unitOfWork,
            IClaimService claimService,
            IAuthService authService,
            IEmailService emailService)
        {
            _unitOfWork  = unitOfWork;
            _claimService = claimService;
            _authService  = authService;
            _emailService = emailService;
        }

        // =====================================================================
        // GetMyCompany
        // =====================================================================
        public async Task<ApiResponse> GetMyCompanyAsync()
        {
            try
            {
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                    return new ApiResponse().SetBadRequest("Phiên đăng nhập không hợp lệ hoặc đã hết hạn.");

                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (user == null || user.CompanyId == null)
                    return new ApiResponse().SetOk((object)null);

                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == user.CompanyId.Value);
                if (company == null)
                    return new ApiResponse().SetOk((object)null);

                return new ApiResponse().SetOk(new CompanyResponse
                {
                    CompanyId   = company.CompanyId,
                    CompanyName = company.CompanyName,
                    Domain      = company.Domain,
                    LogoUrl     = company.LogoUrl
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi khi lấy thông tin công ty: {ex.Message}");
            }
        }

        // =====================================================================
        // InviteEmployee — gửi lời mời qua email, xử lý cả 2 trường hợp
        // =====================================================================
        public async Task<ApiResponse> InviteEmployeeAsync(InviteByEmailDto dto)
        {
            try
            {
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                    return new ApiResponse().SetBadRequest("Phiên đăng nhập không hợp lệ.");

                var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (manager?.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Tài khoản của bạn không thuộc công ty nào.");

                int companyId = manager.CompanyId.Value;

                // ── Kiểm tra quota slot ──────────────────────────────────────
                var activeSubs = await _unitOfWork.Subscriptions.GetAllAsync(
                    s => s.CompanyId == companyId
                      && s.Status == SubscriptionStatus.Active
                      && s.EndDate > DateTime.UtcNow,
                    include: q => q.Include(s => s.SubscriptionPlans));

                var activeSub = activeSubs?.OrderByDescending(s => s.EndDate).FirstOrDefault();
                if (activeSub == null)
                    return new ApiResponse().SetBadRequest("Công ty chưa có gói dịch vụ đang hoạt động. Vui lòng mua gói trước.");

                var plan = activeSub.SubscriptionPlans;
                if (plan != null && activeSub.UsedSlots >= plan.MaxSlots)
                    return new ApiResponse().SetBadRequest($"Gói '{plan.Name}' đã dùng hết {plan.MaxSlots} slot.");

                // ── Kiểm tra lời mời đang chờ xử lý chưa expire ────────────
                var existingPending = await _unitOfWork.CompanyInvitations.GetAsync(
                    i => i.Email.ToLower() == dto.Email.Trim().ToLower()
                      && i.CompanyId == companyId
                      && i.Status == InvitationStatus.Pending
                      && i.ExpiresAt > DateTime.UtcNow);
                if (existingPending != null)
                    return new ApiResponse().SetBadRequest("Đã có lời mời đang chờ xác nhận gửi tới email này. Vui lòng chờ nhân viên phản hồi.");

                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == companyId);

                // ── Phân nhánh: email đã tồn tại hay chưa ──────────────────
                var existingUser = await _unitOfWork.Users.GetAsync(
                    u => u.Email.ToLower() == dto.Email.Trim().ToLower());

                bool isNewUser = existingUser == null;
                int? pendingUserId = null;
                string displayName;
                string tempPassword = string.Empty;

                if (isNewUser)
                {
                    // Tạo user mới, IsActive=false, chờ xác nhận
                    if (string.IsNullOrWhiteSpace(dto.FullName))
                        return new ApiResponse().SetBadRequest("Cần cung cấp họ tên khi mời email chưa có trong hệ thống.");

                    tempPassword = GenerateTempPassword();
                    var pwd = _authService.CreatePasswordHash(tempPassword);

                    var newUser = new User
                    {
                        CompanyId         = null, // sẽ set khi accept
                        RoleId            = 3,
                        Email             = dto.Email.Trim(),
                        FullName          = dto.FullName.Trim(),
                        PasswordHash      = Convert.ToBase64String(pwd.PasswordHash),
                        PasswordSalt      = Convert.ToBase64String(pwd.PasswordSalt),
                        IsActive          = false,   // chờ xác nhận
                        IsEmailVerified   = false,
                        CreatedAt         = DateTime.UtcNow,
                        UpdatedAt         = DateTime.UtcNow,
                    };
                    await _unitOfWork.Users.AddAsync(newUser);
                    await _unitOfWork.SaveChangeAsync();
                    pendingUserId = newUser.UserId;
                    displayName   = dto.FullName.Trim();
                }
                else
                {
                    // Email đã tồn tại
                    if (existingUser!.CompanyId == companyId)
                        return new ApiResponse().SetBadRequest("Nhân viên này đã thuộc công ty của bạn.");
                    if (existingUser.CompanyId != null)
                        return new ApiResponse().SetBadRequest("Email này đã thuộc về công ty khác. Không thể mời.");

                    pendingUserId = existingUser.UserId;
                    displayName   = existingUser.FullName;
                }

                // ── Tạo invitation token ─────────────────────────────────────
                string token = Guid.NewGuid().ToString("N"); // 32 hex chars

                var invitation = new CompanyInvitation
                {
                    CompanyId     = companyId,
                    Email         = dto.Email.Trim().ToLower(),
                    FullName      = displayName,
                    Token         = token,
                    Status        = InvitationStatus.Pending,
                    IsNewUser     = isNewUser,
                    LinkedUserId  = pendingUserId,
                    CreatedAt     = DateTime.UtcNow,
                    ExpiresAt     = DateTime.UtcNow.AddDays(7),
                };
                await _unitOfWork.CompanyInvitations.AddAsync(invitation);
                await _unitOfWork.SaveChangeAsync();

                // ── Gửi email ───────────────────────────────────────────────
                string acceptLink = $"{FE_BASE_URL}/xac-nhan-moi?token={token}";
                string companyName = company?.CompanyName ?? "công ty";

                string emailBody = isNewUser
                    ? $@"<p>Xin chào <b>{displayName}</b>,</p>
<p>Bạn được mời tham gia hệ thống đào tạo an toàn thông tin tại <b>{companyName}</b> trên nền tảng <b>AntiPhisher</b>.</p>
<p>Thông tin tài khoản của bạn:</p>
<ul>
  <li>Email đăng nhập: <b>{dto.Email.Trim()}</b></li>
  <li>Mật khẩu tạm thời: <b>{tempPassword}</b></li>
</ul>
<p>Vui lòng bấm vào nút bên dưới để <b>xác nhận tham gia</b> và kích hoạt tài khoản. Link có hiệu lực trong <b>7 ngày</b>.</p>
<p style=""margin-top:20px"">
  <a href=""{acceptLink}"" style=""background:#4F46E5;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:bold"">
    Xác nhận tham gia
  </a>
</p>
<p style=""margin-top:16px;color:#888;font-size:12px"">Hoặc copy link: {acceptLink}</p>"
                    : $@"<p>Xin chào <b>{displayName}</b>,</p>
<p>Bạn nhận được lời mời tham gia <b>{companyName}</b> trên nền tảng đào tạo an toàn thông tin <b>AntiPhisher</b>.</p>
<p>Bấm vào nút bên dưới để <b>xác nhận tham gia</b>. Link có hiệu lực trong <b>7 ngày</b>.</p>
<p style=""margin-top:20px"">
  <a href=""{acceptLink}"" style=""background:#4F46E5;color:#fff;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:bold"">
    Xác nhận tham gia
  </a>
</p>
<p style=""margin-top:16px;color:#888;font-size:12px"">Hoặc copy link: {acceptLink}</p>";

                var mailResult = await _emailService.SendNotiMail(dto.Email.Trim(), emailBody);
                if (!mailResult.IsSuccess)
                    Console.Error.WriteLine($"[INVITE WARNING] Gửi email thất bại: {mailResult.ErrorMessage}");

                return new ApiResponse().SetOk(new
                {
                    message = $"Đã gửi lời mời tới {dto.Email.Trim()}. Nhân viên cần xác nhận trong vòng 7 ngày.",
                    isNewUser,
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi gửi lời mời: {ex.Message}");
            }
        }

        // =====================================================================
        // AcceptInvitation — nhân viên bấm link trong email
        // =====================================================================
        public async Task<ApiResponse> AcceptInvitationAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return new ApiResponse().SetBadRequest("Token không hợp lệ.");

                var invitation = await _unitOfWork.CompanyInvitations.GetAsync(
                    i => i.Token == token,
                    include: q => q.Include(i => i.Company));

                if (invitation == null)
                    return new ApiResponse().SetNotFound("Lời mời không tồn tại hoặc đã bị xóa.");

                if (invitation.Status == InvitationStatus.Accepted)
                    return new ApiResponse().SetBadRequest("Lời mời này đã được xác nhận trước đó.");

                if (invitation.ExpiresAt < DateTime.UtcNow)
                {
                    invitation.Status = InvitationStatus.Expired;
                    await _unitOfWork.SaveChangeAsync();
                    return new ApiResponse().SetBadRequest("Lời mời đã hết hạn. Vui lòng yêu cầu manager gửi lại.");
                }

                // Tìm user để cập nhật
                var user = invitation.LinkedUserId.HasValue
                    ? await _unitOfWork.Users.GetAsync(u => u.UserId == invitation.LinkedUserId.Value)
                    : await _unitOfWork.Users.GetAsync(u => u.Email.ToLower() == invitation.Email);

                if (user == null)
                    return new ApiResponse().SetNotFound("Không tìm thấy tài khoản liên kết với lời mời này.");

                // Gán vào công ty + kích hoạt (nếu là user mới)
                user.CompanyId       = invitation.CompanyId;
                user.IsActive        = true;
                user.IsEmailVerified = true;
                user.UpdatedAt       = DateTime.UtcNow;

                // Tăng UsedSlots trên subscription
                var activeSubs = await _unitOfWork.Subscriptions.GetAllAsync(
                    s => s.CompanyId == invitation.CompanyId
                      && s.Status == SubscriptionStatus.Active
                      && s.EndDate > DateTime.UtcNow,
                    include: q => q.Include(s => s.SubscriptionPlans));

                var activeSub = activeSubs?.OrderByDescending(s => s.EndDate).FirstOrDefault();
                if (activeSub != null)
                {
                    activeSub.UsedSlots++;
                    activeSub.ModifiedDate = DateTime.UtcNow;
                }

                invitation.Status     = InvitationStatus.Accepted;
                invitation.AcceptedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();

                return new ApiResponse().SetOk(new
                {
                    message     = $"Xác nhận thành công! Bạn đã được thêm vào {invitation.Company?.CompanyName ?? "công ty"}.",
                    companyName = invitation.Company?.CompanyName,
                    isNewUser   = invitation.IsNewUser,
                    email       = invitation.Email,
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi xử lý xác nhận: {ex.Message}");
            }
        }

        // =====================================================================
        // UpdateCompanyName
        // =====================================================================
        public async Task<ApiResponse> UpdateCompanyNameAsync(string companyName)
        {
            try
            {
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                    return new ApiResponse().SetBadRequest("Phiên đăng nhập không hợp lệ.");

                var user = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (user?.CompanyId == null)
                    return new ApiResponse().SetBadRequest("Bạn chưa được gán vào công ty nào.");

                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == user.CompanyId.Value);
                if (company == null)
                    return new ApiResponse().SetBadRequest("Không tìm thấy công ty.");

                company.CompanyName = companyName.Trim();
                company.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangeAsync();

                return new ApiResponse().SetOk(new { message = "Đã cập nhật tên công ty.", companyName = company.CompanyName });
            }
            catch (Exception ex)
            {
                return new ApiResponse().SetBadRequest($"Lỗi cập nhật tên công ty: {ex.Message}");
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================
        private static string GenerateTempPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var rng = RandomNumberGenerator.Create();
            var bytes = new byte[10];
            rng.GetBytes(bytes);
            var sb = new StringBuilder();
            foreach (var b in bytes)
                sb.Append(chars[b % chars.Length]);
            return sb.ToString();
        }
    }
}
