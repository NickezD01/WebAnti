using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.AIKnowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/ai-knowledge")]
    [Authorize(Roles = "Admin,Manager")]
    public class AIKnowledgeController : ControllerBase
    {
        private readonly IAIKnowledgeService _knowledgeService;
        private readonly IClaimService _claim;

        public AIKnowledgeController(IAIKnowledgeService knowledgeService, IClaimService claim)
        {
            _knowledgeService = knowledgeService;
            _claim = claim;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAIKnowledgeRequest request)
        {
            var claim = _claim.GetUserClaim();
            var result = await _knowledgeService.CreateAsync(request, claim.Id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _knowledgeService.GetAllActiveAsync();
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByTag([FromQuery] string tag)
        {
            var result = await _knowledgeService.SearchByTagAsync(tag);
            return Ok(result);
        }
    }
}