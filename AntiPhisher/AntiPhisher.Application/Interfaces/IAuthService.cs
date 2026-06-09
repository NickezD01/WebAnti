using AntiPhisher.Application.Request.UserAccount;
using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse> RegisterAsync(UserRegisterRequest userRequest);
        Task<ApiResponse> LoginAsync(LoginRequest request);
        Task<ApiResponse> LoginWithGoogleAsync(GoogleLoginRequest request);
        Task<ApiResponse> VerifyEmailAsync(int userId, string verificationCode);
        Task<ApiResponse> ChangePasswordAsync(ChangePasswordRequest request, int userId);
        Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);

        // Đã đồng bộ kiểu trả về PasswordDTO theo cấu trúc thực tế của AuthService
        Services.AuthService.PasswordDTO CreatePasswordHash(string password);

    }
}
