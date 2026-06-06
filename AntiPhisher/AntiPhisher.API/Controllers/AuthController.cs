using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.UserAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        public IAuthService _service;
        private readonly IClaimService _claimService;

        public AuthController(IAuthService service, IClaimService claimService)
        {
            _service = service;
            _claimService = claimService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterRequest user)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new
                {
                    statusCode = 400,
                    isSuccess = false,
                    errorMessage = string.Join("; ", errors),
                    result = (object)null
                });
            }
            var result = await _service.RegisterAsync(user);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest user)
        {
            var result = await _service.LoginAsync(user);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            var result = await _service.LoginWithGoogleAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("Verification")]
        public async Task<IActionResult> Verification(VerificationEmailRequest request)
        {

            var result = await _service.VerifyEmailAsync(request.UserId, request.VerificationCode);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = _claimService.GetUserClaim().Id;
            var result = await _service.ChangePasswordAsync(request, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _service.ForgotPasswordAsync(request);
            return Ok(result); // luôn 200 — tránh email enumeration
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _service.ResetPasswordAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
