using AntiPhisher.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IClaimService _claimService;

        public AnalyticsController(IAnalyticsService analyticsService, IClaimService claimService)
        {
            _analyticsService = analyticsService;
            _claimService = claimService;
        }

        /// <summary>
        /// Tổng quan hiệu suất bảo mật toàn công ty.
        /// Gồm: avg risk score, detection rate, lesson completion, heatmap click nhầm (UTC+7).
        /// GET /api/Analytics/company-overview
        /// </summary>
        [HttpGet("company-overview")]
        public async Task<IActionResult> GetCompanyOverview()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _analyticsService.GetCompanyOverviewAsync(claim.Id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Danh sách nhân viên nguy hiểm nhất (risk score thấp nhất lên đầu).
        /// Gồm: risk score, detection rate, số lần click/leak/report, tiến độ bài học.
        /// GET /api/Analytics/high-risk-employees
        /// </summary>
        [HttpGet("high-risk-employees")]
        public async Task<IActionResult> GetHighRiskEmployees()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _analyticsService.GetHighRiskEmployeesAsync(claim.Id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Tiến độ từng Campaign: lesson completion % + attempt score + per-user breakdown.
        /// GET /api/Analytics/campaign-completion
        /// </summary>
        [HttpGet("campaign-completion")]
        public async Task<IActionResult> GetCampaignCompletion()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _analyticsService.GetCampaignCompletionAsync(claim.Id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
