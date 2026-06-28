using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.UserAccount;
using AntiPhisher.Application.Response;
using AntiPhisher.Domain;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AntiPhisher.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly AppSetting _appSettings;

        private readonly IEmailService _emailService;

        public AuthService(
            IUnitOfWork unitOfWork,
            AppSetting appSettings,
            IEmailService emailService
        )
        {
            _unitOfWork = unitOfWork;

            _appSettings = appSettings;

            _emailService = emailService;
        }

        // =====================================================
        // REGISTER
        // =====================================================

        public async Task<ApiResponse> RegisterAsync(
            UserRegisterRequest userRequest
        )
        {
            ApiResponse response = new ApiResponse();

            try
            {
                // CHECK EMAIL EXIST
                var existingUser =
                    await _unitOfWork.Users.GetAsync(
                        x => x.Email == userRequest.Email
                    );

                if (existingUser != null)
                {
                    if (existingUser.IsEmailVerified)
                    {
                        response.SetBadRequest("Email already exists");
                        return response;
                    }
                    else
                    {
                        // Remove unverified user to let them register again
                        var oldVerifications = await _unitOfWork.EmailVerifications.GetAllAsync(x => x.UserId == existingUser.UserId);
                        foreach (var v in oldVerifications)
                        {
                            _unitOfWork.EmailVerifications.Remove(v);
                        }
                        _unitOfWork.Users.Remove(existingUser);
                        await _unitOfWork.SaveChangeAsync();
                    }
                }

                // Default RoleId for new User is 3
                int defaultRoleId = 3;

                // HASH PASSWORD
                var passwordData =
                    CreatePasswordHash(userRequest.Password);

                // CREATE USER
                User user = new User
                {
                    FullName = userRequest.FullName,
                    Email = userRequest.Email,
                    PasswordHash = Convert.ToBase64String(passwordData.PasswordHash),
                    PasswordSalt = Convert.ToBase64String(passwordData.PasswordSalt),
                    RoleId = defaultRoleId,
                    CompanyId = null,
                    AvatarUrl = null,
                    IsActive = true,
                    IsEmailVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Users.AddAsync(user);

                await _unitOfWork.SaveChangeAsync();

                // CREATE VERIFY CODE
                string verificationCode =
                    GenerateVerificationCode();

                EmailVerification emailVerification =
                    new EmailVerification
                    {
                        UserId = user.UserId,

                        VerificationCode = verificationCode,

                        ExpiresAt =
                            DateTime.UtcNow.AddMinutes(30),

                        IsUsed = false
                    };

                await _unitOfWork.EmailVerifications
                    .AddAsync(emailVerification);

                await _unitOfWork.SaveChangeAsync();

                // SEND EMAIL
                string emailContent =
                    $@"
                    Dear {user.FullName},
                    <br/><br/>
                    Your verification code is:
                    <strong>{verificationCode}</strong>
                    <br/><br/>
                    This code will expire in 30 minutes.
                    ";

                var emailResult =
                    await _emailService.SendValidationEmail(
                        user.Email,
                        emailContent
                    );

                if (!emailResult.IsSuccess)
                {
                    response.SetBadRequest(
                        "Send email failed"
                    );

                    return response;
                }

                response.SetOk(
                    new
                    {
                        user.UserId,
                        Message = "Register success"
                    }
                );

                return response;
            }
            catch (Exception ex)
            {
                response.SetBadRequest(
                    $"Error: {ex.Message}"
                );

                return response;
            }
        }

        // =====================================================
        // LOGIN
        // =====================================================

        public async Task<ApiResponse> LoginAsync(
            LoginRequest request
        )
        {
            ApiResponse response = new ApiResponse();

            try
            {
                // GET USER + ROLE
                var user = await _unitOfWork.Users.GetAsync(
                x => x.Email == request.UserEmail,
                include: source => source.Include(x => x.Role)
            );

                if (user == null)
                {
                    response.SetBadRequest(
                        "Email or password is incorrect"
                    );

                    return response;
                }

                // VERIFY PASSWORD
                bool isPasswordCorrect =
                    VerifyPasswordHash(
                        request.Password,
                        Convert.FromBase64String(
                            user.PasswordHash
                        ),
                        Convert.FromBase64String(
                            user.PasswordSalt
                        )
                    );

                if (!isPasswordCorrect)
                {
                    response.SetBadRequest(
                        "Email or password is incorrect"
                    );

                    return response;
                }

                // CHECK ACCOUNT ACTIVE
                if (!user.IsActive)
                {
                    response.SetBadRequest(
                        "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên."
                    );

                    return response;
                }

                // CHECK VERIFY EMAIL
                if (!user.IsEmailVerified)
                {
                    response.SetBadRequest(
                        "Please verify your email first"
                    );

                    return response;
                }

                // CREATE TOKEN
                string token = CreateToken(user);

                response.SetOk(token);

                return response;
            }
            catch (Exception ex)
            {
                response.SetBadRequest(
                    $"Error: {ex.Message}"
                );

                return response;
            }
        }

        // =====================================================
        // LOGIN WITH GOOGLE
        // =====================================================

        public async Task<ApiResponse> LoginWithGoogleAsync(GoogleLoginRequest request)
        {
            ApiResponse response = new ApiResponse();

            try
            {
                // Verify Google token
                var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(request.Credential, new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                {
                    // You can specify your Client ID here or let it validate signature without checking audience.
                    Audience = new[] { "87250051752-p12lltrop700iehbui5pije6r1h6jn4e.apps.googleusercontent.com" }
                });

                if (payload == null)
                {
                    response.SetBadRequest("Invalid Google token");
                    return response;
                }

                // GET USER
                var user = await _unitOfWork.Users.GetAsync(
                    x => x.Email == payload.Email,
                    include: source => source.Include(x => x.Role)
                );

                if (user == null)
                {
                    // CREATE NEW USER IF NOT EXISTS
                    var passwordData = CreatePasswordHash(Guid.NewGuid().ToString());
                    int defaultRoleId = 3; // Use 3 for User Role

                    user = new User
                    {
                        FullName = payload.Name ?? "Google User",
                        Email = payload.Email,
                        PasswordHash = Convert.ToBase64String(passwordData.PasswordHash),
                        PasswordSalt = Convert.ToBase64String(passwordData.PasswordSalt),
                        RoleId = defaultRoleId,
                        AvatarUrl = payload.Picture,
                        IsActive = true,
                        IsEmailVerified = true, // Trusted from Google
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.Users.AddAsync(user);
                    await _unitOfWork.SaveChangeAsync();

                    // Re-fetch to include role
                    user = await _unitOfWork.Users.GetAsync(
                        x => x.Email == payload.Email,
                        include: source => source.Include(x => x.Role)
                    );
                }

                // CREATE TOKEN
                string token = CreateToken(user);
                response.SetOk(token);

                return response;
            }
            catch (Exception ex)
            {
                response.SetBadRequest($"Google login error: {ex.Message}");
                return response;
            }
        }

        // =====================================================
        // VERIFY EMAIL
        // =====================================================

        public async Task<ApiResponse> VerifyEmailAsync(
            int userId,
            string verificationCode
        )
        {
            ApiResponse response = new ApiResponse();

            try
            {
                var verify =
                    await _unitOfWork.EmailVerifications
                    .GetAsync(
                        x =>
                            x.UserId == userId
                            &&
                            x.VerificationCode
                                == verificationCode
                            &&
                            !x.IsUsed
                    );

                if (verify == null)
                {
                    response.SetBadRequest(
                        "Invalid verification code"
                    );

                    return response;
                }

                if (verify.ExpiresAt < DateTime.UtcNow)
                {
                    response.SetBadRequest(
                        "Verification code expired"
                    );

                    return response;
                }

                verify.IsUsed = true;

                var user =
                    await _unitOfWork.Users.GetAsync(
                        x => x.UserId == userId
                    );

                if (user == null)
                {
                    response.SetBadRequest(
                        "User not found"
                    );

                    return response;
                }

                user.IsEmailVerified = true;

                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();

                response.SetOk(
                    "Email verified successfully"
                );

                return response;
            }
            catch (Exception ex)
            {
                response.SetBadRequest(
                    $"Error: {ex.Message}"
                );

                return response;
            }
        }

        // =====================================================
        // CREATE TOKEN
        // =====================================================

        private string CreateToken(User user)
        {
            string roleName =
                user.Role?.RoleName ?? "User";

            List<Claim> claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString()
                ),

                new Claim(
                    "UserId",
                    user.UserId.ToString()
                ),

                new Claim(
                    ClaimTypes.Email,
                    user.Email
                ),

                new Claim(
                    "Email",
                    user.Email
                ),

                new Claim(
                    "FullName",
                    user.FullName
                ),

                new Claim(
                    ClaimTypes.Name,
                    user.FullName
                ),

                new Claim(
                    ClaimTypes.Role,
                    roleName
                ),

                new Claim(
                    "Role",
                    roleName
                )
            };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _appSettings.SecretToken.Value
                    )
                );

            var creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha512
                );

            var token =
                new JwtSecurityToken(
                    claims: claims,
                    expires:
                        DateTime.UtcNow.AddDays(1),
                    signingCredentials: creds
                );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }

        // =====================================================
        // PASSWORD HASH
        // =====================================================

        public PasswordDTO CreatePasswordHash(
            string password
        )
        {
            PasswordDTO passwordDTO =
                new PasswordDTO();

            using (var hmac = new HMACSHA512())
            {
                passwordDTO.PasswordSalt =
                    hmac.Key;

                passwordDTO.PasswordHash =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(password)
                    );
            }

            return passwordDTO;
        }

        private bool VerifyPasswordHash(
            string password,
            byte[] storedHash,
            byte[] storedSalt
        )
        {
            using (
                var hmac =
                    new HMACSHA512(storedSalt)
            )
            {
                var computedHash =
                    hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(password)
                    );

                return computedHash
                    .SequenceEqual(storedHash);
            }
        }

        // =====================================================
        // GENERATE VERIFY CODE
        // =====================================================

        private string GenerateVerificationCode()
        {
            Random random = new Random();

            return random
                .Next(100000, 999999)
                .ToString();
        }

        // =====================================================
        // PASSWORD DTO
        // =====================================================

        public class PasswordDTO
        {
            public byte[] PasswordHash { get; set; }
                = new byte[64];

            public byte[] PasswordSalt { get; set; }
                = new byte[128];
        }

        // =====================================================
        // CHANGE PASSWORD
        // =====================================================

        public async Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request, int userId)
        {
            var response = new ApiResponse();
            try
            {
                // 1. User tồn tại không
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == userId);
                if (user == null)
                    return response.SetNotFound("Không tìm thấy tài khoản.");

                // 2. Độ dài mật khẩu mới
                if (request.NewPassword.Length < 6)
                    return response.SetBadRequest("Mật khẩu mới phải có ít nhất 6 ký tự.");

                // 3. Xác nhận khớp
                if (request.NewPassword != request.ConfirmPassword)
                    return response.SetBadRequest("Mật khẩu xác nhận không khớp.");

                // 4. Mật khẩu mới không được trùng cũ
                if (request.NewPassword == request.CurrentPassword)
                    return response.SetBadRequest("Mật khẩu mới không được trùng mật khẩu hiện tại.");

                // 5. Verify mật khẩu hiện tại
                var storedHash = Convert.FromBase64String(user.PasswordHash);
                var storedSalt = Convert.FromBase64String(user.PasswordSalt);
                if (!VerifyPasswordHash(request.CurrentPassword, storedHash, storedSalt))
                    return response.SetBadRequest("Mật khẩu hiện tại không đúng.");

                // 6. Hash + lưu mật khẩu mới
                var pwd = CreatePasswordHash(request.NewPassword);
                user.PasswordHash = Convert.ToBase64String(pwd.PasswordHash);
                user.PasswordSalt = Convert.ToBase64String(pwd.PasswordSalt);
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangeAsync();
                return response.SetOk("Đổi mật khẩu thành công.");
            }
            catch (Exception ex)
            {
                return response.SetBadRequest($"Lỗi khi đổi mật khẩu: {ex.Message}");
            }
        }

        // =====================================================
        // FORGOT PASSWORD
        // =====================================================

        public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var response = new ApiResponse();
            const string ALWAYS_OK = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được link đặt lại mật khẩu trong vài phút.";
            try
            {
                var user = await _unitOfWork.Users.GetAsync(x => x.Email == request.Email);

                // Always return 200 — don't reveal whether email exists
                if (user == null)
                    return response.SetOk(ALWAYS_OK);

                // Invalidate all pending (unused) tokens for this user
                var oldTokens = await _unitOfWork.PasswordResetTokens.GetAllAsync(
                    x => x.UserId == user.UserId && x.UsedAt == null);
                foreach (var t in oldTokens)
                    t.UsedAt = DateTime.UtcNow;

                // Generate crypto-safe plain token → send in link
                byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
                string plainToken = Convert.ToBase64String(randomBytes); // 44 chars

                // SHA256(plainToken) → store in DB
                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainToken));
                string tokenHash = Convert.ToBase64String(hashBytes); // 44 chars

                await _unitOfWork.PasswordResetTokens.AddAsync(new PasswordResetToken
                {
                    UserId = user.UserId,
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                    CreatedAt = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangeAsync();

                // plainToken is Base64 — Uri.EscapeDataString handles +, /, = safely
                // FE reads ?token= via useSearchParams which auto-decodes → no double-decode needed
                var resetLink = $"{_appSettings.FrontendUrl}/dat-lai-mat-khau?token={Uri.EscapeDataString(plainToken)}";

                string emailContent = $@"
                    Dear {user.FullName},<br/><br/>
                    Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <strong>{user.Email}</strong>.<br/>
                    Click vào link bên dưới để đặt lại mật khẩu (hiệu lực 30 phút):<br/><br/>
                    <a href=""{resetLink}"">{resetLink}</a><br/><br/>
                    Nếu bạn không yêu cầu điều này, hãy bỏ qua email này.
                ";

                var emailResult = await _emailService.SendNotiMail(user.Email, emailContent);
                if (!emailResult.IsSuccess)
                    Console.Error.WriteLine($"[FORGOT PWD WARNING] SendNotiMail fail: {emailResult.ErrorMessage}");

                return response.SetOk(ALWAYS_OK);
            }
            catch (Exception ex)
            {
                return response.SetBadRequest($"Lỗi: {ex.Message}");
            }
        }

        // =====================================================
        // RESET PASSWORD
        // =====================================================

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var response = new ApiResponse();
            try
            {
                // 1. Hash the received plain token (same way A2 stores it)
                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(request.Token));
                string tokenHash = Convert.ToBase64String(hashBytes);

                // 2. Verify: valid hash, not used, not expired — gộp chung mọi case sai/hết hạn/đã dùng
                var tokenRecord = await _unitOfWork.PasswordResetTokens.GetAsync(
                    x => x.TokenHash == tokenHash
                      && x.UsedAt == null
                      && x.ExpiresAt > DateTime.UtcNow);
                if (tokenRecord == null)
                    return response.SetBadRequest("Link đặt lại không hợp lệ hoặc đã hết hạn.");

                // 3. Validate password
                if (request.NewPassword.Length < 6)
                    return response.SetBadRequest("Mật khẩu phải có ít nhất 6 ký tự.");
                if (request.NewPassword != request.ConfirmPassword)
                    return response.SetBadRequest("Mật khẩu xác nhận không khớp.");

                // 4. Get user (FK guarantees existence)
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == tokenRecord.UserId);

                // 5. Hash new password — reuse CreatePasswordHash (private, same class)
                var pwd = CreatePasswordHash(request.NewPassword);
                user.PasswordHash = Convert.ToBase64String(pwd.PasswordHash);
                user.PasswordSalt = Convert.ToBase64String(pwd.PasswordSalt);
                user.UpdatedAt = DateTime.UtcNow;

                // 6. Invalidate ALL unused tokens for this user (incl. current) — chặn replay & multi-token
                var allUnused = await _unitOfWork.PasswordResetTokens.GetAllAsync(
                    x => x.UserId == tokenRecord.UserId && x.UsedAt == null);
                foreach (var t in allUnused)
                    t.UsedAt = DateTime.UtcNow;

                // 7. Persist user + tokens in one shot
                await _unitOfWork.SaveChangeAsync();
                return response.SetOk("Đặt lại mật khẩu thành công. Vui lòng đăng nhập.");
            }
            catch (Exception ex)
            {
                return response.SetBadRequest($"Lỗi: {ex.Message}");
            }
        }

        // =====================================================
        // UNUSED
        // =====================================================

    }
}