using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CampaignGenerator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/campaign-generator")]
    [Authorize(Roles = "Admin,Manager")]
    public class CampaignGeneratorController : ControllerBase
    {
        private readonly ICampaignGeneratorService _generatorService;
        private readonly IClaimService _claim;

        public CampaignGeneratorController(ICampaignGeneratorService generatorService, IClaimService claim)
        {
            _generatorService = generatorService;
            _claim = claim;
        }

        /// <summary>
        /// Sinh preview Scenario mới từ 1 context đã có trong AIExpandedKnowledge.
        /// Chưa lưu vào DB — Admin xem trước rồi mới quyết định lưu (Bước 4.4).
        /// </summary>
        [HttpPost("preview")]
        public async Task<IActionResult> GeneratePreview([FromQuery] int knowledgeId, [FromQuery] int difficultyId)
        {
            var preview = await _generatorService.GeneratePreviewAsync(knowledgeId, difficultyId);
            return Ok(preview);
        }

        /// <summary>
        /// Lưu preview đã được Admin xác nhận (có thể đã tự sửa) thành Scenario thật.
        /// Scenario mới sẽ ở trạng thái IsActive=false, GenerationStatus="PendingReview"
        /// — cần 1 bước duyệt riêng để chính thức đưa vào pool sử dụng.
        /// </summary>
        [HttpPost("save")]
        public async Task<IActionResult> SaveAsScenario([FromBody] SaveGeneratedScenarioRequest request)
        {
            var claim = _claim.GetUserClaim();
            var result = await _generatorService.SaveAsScenarioAsync(request, claim.Id);
            return Ok(result);
        }
    }
}