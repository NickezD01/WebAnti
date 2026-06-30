using AntiPhisher.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificateController : ControllerBase
    {
        private readonly ICertificateService _certificateService;
        private readonly IClaimService _claimService;

        public CertificateController(ICertificateService certificateService, IClaimService claimService)
        {
            _certificateService = certificateService;
            _claimService = claimService;
        }

        /// <summary>
        /// GET /api/Certificate/mine
        /// Trả về chứng chỉ hiện tại của user đang đăng nhập (404 nếu chưa có).
        /// </summary>
        [Authorize(Roles = "User,Manager,Admin")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMine()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _certificateService.GetMyCertificateAsync(claim.Id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// POST /api/Certificate/issue
        /// Phát hành chứng chỉ nếu đủ điều kiện (≥10 lần, ≥70% đúng).
        /// Nếu đã có rồi thì trả về chứng chỉ cũ.
        /// </summary>
        [Authorize(Roles = "User,Manager,Admin")]
        [HttpPost("issue")]
        public async Task<IActionResult> Issue()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _certificateService.IssueOrGetCertificateAsync(claim.Id, claim.Name);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// GET /api/Certificate/verify/{code}
        /// Xác minh chứng chỉ công khai (không cần đăng nhập).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("verify/{code}")]
        public async Task<IActionResult> Verify(string code)
        {
            var result = await _certificateService.VerifyCodeAsync(code);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }
    }
}
