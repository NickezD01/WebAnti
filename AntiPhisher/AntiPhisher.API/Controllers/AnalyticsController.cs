using AntiPhisher.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
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
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-overview")]
        public async Task<IActionResult> GetAdminOverview()
        {
            var result = await _analyticsService.GetAdminOverviewAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Manager")]
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
        [Authorize(Roles = "Manager")]
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
        [Authorize(Roles = "Manager")]
        [HttpGet("campaign-completion")]
        public async Task<IActionResult> GetCampaignCompletion()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _analyticsService.GetCampaignCompletionAsync(claim.Id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpGet("my-report")]
        public async Task<IActionResult> GetMyReport()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _analyticsService.GetMyReportAsync(claim.Id);
            return Ok(result);
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("company-leaderboard")]
        public async Task<IActionResult> GetCompanyLeaderboard()
        {
            var claim = _claimService.GetUserClaim();
            var result = await _analyticsService.GetCompanyLeaderboardAsync(claim.Id);
            return Ok(result);
        }
    }
}
