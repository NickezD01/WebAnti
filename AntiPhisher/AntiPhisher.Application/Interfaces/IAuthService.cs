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

    }
}
