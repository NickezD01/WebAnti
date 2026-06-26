using System.Threading.Tasks;
using AntiPhisher.Application;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response.OrgReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/org-report")]
    [Authorize(Roles = "Admin,Manager")]
    public class OrgReportController : ControllerBase
    {
        private readonly IOrgReportService _orgReportService;
        private readonly IClaimService _claim;
        private readonly IUnitOfWork _unitOfWork;

        public OrgReportController(
            IOrgReportService orgReportService,
            IClaimService claim,
            IUnitOfWork unitOfWork)
        {
            _orgReportService = orgReportService;
            _claim = claim;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Manager: tự động lấy báo cáo công ty của chính họ (không cần truyền companyId).
        /// Admin: bắt buộc truyền companyId để xem báo cáo công ty cụ thể.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrgReport([FromQuery] int? companyId, [FromQuery] bool forceRefresh = false)
        {
            var claim = _claim.GetUserClaim();

            int targetCompanyId;

            if (claim.Role == "Manager")
            {
                var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == claim.Id);
                if (manager?.CompanyId == null)
                    return BadRequest("Bạn chưa được gán vào công ty nào.");

                targetCompanyId = manager.CompanyId.Value;
            }
            else // Admin
            {
                if (companyId == null)
                    return BadRequest("Vui lòng cung cấp companyId để xem báo cáo.");

                targetCompanyId = companyId.Value;
            }

            var result = await _orgReportService.GetOrgReportAsync(targetCompanyId, forceRefresh);

            var response = new OrgReportResponse
            {
                CompanyId = result.Stats.CompanyId,
                CompanyName = result.Stats.CompanyName,
                TotalEmployees = result.Stats.TotalEmployees,
                TotalAttempts = result.Stats.TotalAttempts,
                TotalFlaws = result.Stats.TotalFlaws,
                OrgFailRate = result.Stats.OrgFailRate,
                TacticBreakdown = result.Stats.TacticBreakdown.ConvertAll(t => new OrgTacticStatResponse
                {
                    CategoryName = t.CategoryName,
                    TotalExposed = t.TotalExposed,
                    TotalFailed = t.TotalFailed,
                    FailRate = t.FailRate
                }),
                HighRiskUsers = result.Stats.HighRiskUsers.ConvertAll(h => new HighRiskUserResponse
                {
                    UserId = h.UserId,
                    FullName = h.FullName,
                    RiskScore = h.RiskScore,
                    TotalFlaws = h.TotalFlaws
                }),
                ExecutiveSummary = result.AiSummary.ExecutiveSummary,
                TopRisks = result.AiSummary.TopRisks,
                SuggestedNextCampaignContext = result.AiSummary.SuggestedNextCampaignContext,
                SuggestedDifficulty = result.AiSummary.SuggestedDifficulty
            };

            return Ok(response);
        }
    }
}