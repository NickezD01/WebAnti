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

        private PasswordDTO CreatePasswordHash(
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
        // UNUSED
        // =====================================================

    }
}