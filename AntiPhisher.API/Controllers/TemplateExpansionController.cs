using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.TemplateExpansion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/template-expansion")]
    [Authorize(Roles = "Admin,Manager")]
    public class TemplateExpansionController : ControllerBase
    {
        private readonly ITemplateExpansionService _expansionService;
        private readonly IClaimService _claim;

        public TemplateExpansionController(ITemplateExpansionService expansionService, IClaimService claim)
        {
            _expansionService = expansionService;
            _claim = claim;
        }

        /// <summary>
        /// Sinh preview 1 Scenario biến thể mới, dựa trên các Scenario đã có (Manual/Approved)
        /// cùng mức độ khó (và category nếu lọc thêm). Chưa lưu vào DB.
        /// </summary>
        [HttpPost("generate-similar")]
        public async Task<IActionResult> GenerateSimilar([FromBody] TemplateExpansionRequest request)
        {
            var preview = await _expansionService.GenerateSimilarPreviewAsync(request);
            return Ok(preview);
        }
        [HttpPost("save")]
        public async Task<IActionResult> SaveAsScenario([FromBody] SaveSimilarScenarioRequest request)
        {
            var claim = _claim.GetUserClaim();
            var result = await _expansionService.SaveAsScenarioAsync(request, claim.Id);
            return Ok(result);
        }
    }
}