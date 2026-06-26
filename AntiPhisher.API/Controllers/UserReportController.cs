using AntiPhisher.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/user-report")]
    [Authorize] // mọi user đã đăng nhập chỉ xem được advice của chính họ
    public class UserReportController : ControllerBase
    {
        private readonly IUserFlawSummaryService _flawSummaryService;
        private readonly IClaimService _claim;

        public UserReportController(IUserFlawSummaryService flawSummaryService, IClaimService claim)
        {
            _flawSummaryService = flawSummaryService;
            _claim = claim;
        }

        /// <summary>
        /// Lấy báo cáo AI cá nhân hóa của user đang đăng nhập.
        /// Mặc định dùng cache nếu còn hợp lệ (24h); set forceRefresh=true để bắt buộc gọi AI lại.
        /// </summary>
        [HttpGet("predictive-advice")]
        public async Task<IActionResult> GetMyPredictiveAdvice([FromQuery] bool forceRefresh = false)
        {
            var claim = _claim.GetUserClaim();
            if (claim == null)
                return Unauthorized("Không tìm thấy thông tin xác thực.");

            var advice = await _flawSummaryService.GetAdviceAsync(claim.Id, forceRefresh);
            return Ok(advice);
        }
    }
}