using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.ScenarioReview;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/scenario-review")]
    [Authorize(Roles = "Admin,Manager")]
    public class ScenarioReviewController : ControllerBase
    {
        private readonly IScenarioReviewService _reviewService;
        private readonly IClaimService _claim;

        public ScenarioReviewController(IScenarioReviewService reviewService, IClaimService claim)
        {
            _reviewService = reviewService;
            _claim = claim;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingReview()
        {
            var result = await _reviewService.GetPendingReviewAsync();
            return Ok(result);
        }

        [HttpPost("{scenarioId}/approve")]
        public async Task<IActionResult> Approve(int scenarioId)
        {
            var claim = _claim.GetUserClaim();
            var result = await _reviewService.ApproveAsync(scenarioId, claim.Id);
            return Ok(result);
        }

        [HttpPost("{scenarioId}/reject")]
        public async Task<IActionResult> Reject(int scenarioId, [FromBody] RejectScenarioRequest request)
        {
            var claim = _claim.GetUserClaim();
            var result = await _reviewService.RejectAsync(scenarioId, request.Reason, claim.Id);
            return Ok(result);
        }
    }
}