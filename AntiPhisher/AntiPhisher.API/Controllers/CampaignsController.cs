using System;
using System.Threading.Tasks;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CampaignRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Services;
using AntiPhisher.Domain.Models;
using AntiPhisher.Infrastructure;
using AntiPhisher.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
namespace AntiPhisher.API.Controllers
{
    // ✅ DTO và Record đặt NGOÀI class, TRONG namespace
    public class SubmitAnswerRequest
    {
        public int CampaignId { get; set; }
        public string UserAction { get; set; }
    }

    public record UpdateCampaignStatusRequest(bool IsActive);

    // ✅ Chỉ có DUY NHẤT một class CampaignsController
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly IClaimService _claimService;
        private readonly AppDbContext _context;
        private readonly IOpenRouterAnalysisService _aiService;

        // ✅ Một constructor duy nhất, assign đủ 4 field
        public CampaignsController(
            ICampaignService campaignService,
            IClaimService claimService,
            AppDbContext context,
            IOpenRouterAnalysisService aiService)
         {
            _campaignService = campaignService;
            _claimService = claimService;
            _context = context;
            _aiService = aiService;
         }

        // ── Endpoint cũ 1 ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAllCampaigns()
        {
            var response = new ApiResponse();
            try
            {
                var campaigns = await _campaignService.GetAllCampaignsAsync();
                response.SetOk(campaigns);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // ── Endpoint cũ 2 ──────────────────────────────────
        [HttpGet("my-campaigns")]
        public async Task<IActionResult> GetMyCampaigns()
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var campaigns = await _campaignService.GetMyCampaignsAsync(claim.Id);
                response.SetOk(campaigns);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // ── Endpoint cũ 3 ──────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCampaignById(int id)
        {
            var response = new ApiResponse();
            try
            {
                var campaign = await _campaignService.GetCampaignByIdAsync(id);
                if (campaign == null)
                {
                    response.SetNotFound(message: $"Không tìm thấy chiến dịch ID {id}");
                    return NotFound(response);
                }
                response.SetOk(campaign);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // ── Endpoint cũ 4 ──────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var createdCampaign = await _campaignService.CreateCampaignAsync(request, claim.Id, claim.Role);
                response.SetOk(createdCampaign);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // ── Endpoint cũ 5 ──────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                await _campaignService.DeleteCampaignAsync(id, claim.Id, claim.Role);
                response.SetOk(new { Message = $"Đã xóa thành công chiến dịch ID {id}" });
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                response.SetNotFound(message: ex.Message);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // ── Endpoint cũ 6 ──────────────────────────────────
        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateCampaignStatus(
            int id, [FromBody] UpdateCampaignStatusRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var updated = await _campaignService.UpdateCampaignStatusAsync(
                    id, request.IsActive, claim.Id, claim.Role);
                response.SetOk(updated);
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                response.SetNotFound(message: ex.Message);
                return NotFound(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        // ── Endpoint mới: AI chấm bài ──────────────────────
        [HttpPost("submit-answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                if (claim == null)
                    return Unauthorized(new { message = "Vui lòng đăng nhập." });

                // ✅ Query thẳng vào CampaignScenarios → Scenario
                // Bỏ qua Campaign entity, tránh lỗi Include không load được
                var campaignScenario = await _context.CampaignScenarios
                    .Include(cs => cs.Scenario)
                    .Where(cs => cs.CampaignId == request.CampaignId)
                    .OrderBy(cs => cs.OrderIndex)
                    .FirstOrDefaultAsync();

                if (campaignScenario == null || campaignScenario.Scenario == null)
                {
                    response.SetNotFound(
                        message: $"Không tìm thấy kịch bản cho chiến dịch ID {request.CampaignId}");
                    return NotFound(response);
                }

                var scenario = campaignScenario.Scenario;

                // Ghép context email đầy đủ cho AI
                string emailContext = $"""
                    Tiêu đề: {scenario.Subject}
                    Người gửi: {scenario.SenderName} <{scenario.SenderEmail}>
                    Nội dung email:
                    {scenario.EmailBodyHtml}
                    Đây có phải email lừa đảo không: {(scenario.IsPhishing ? "Có" : "Không")}
                    """;

                string aiRawJson = await _aiService.AnalyzeCampaignActionAsync(
                    emailContext,
                    request.UserAction);

                //  Làm sạch nếu Gemini wrap trong ```json ... ```
                aiRawJson = aiRawJson?.Trim() ?? "";
                if (aiRawJson.StartsWith("```"))
                {
                    var lines = aiRawJson.Split('\n').ToList();
                    lines.RemoveAt(0);
                    if (lines.Count > 0 && lines.Last().Trim() == "```")
                        lines.RemoveAt(lines.Count - 1);
                    aiRawJson = string.Join('\n', lines).Trim();
                }

                //  Dùng System.Text.Json thay vì Newtonsoft để parse chính xác hơn
                using var aiDoc = System.Text.Json.JsonDocument.Parse(aiRawJson);
                var aiRoot = aiDoc.RootElement;

                bool isCorrect = aiRoot.GetProperty("isCorrect").GetBoolean();
                string detectedFlaw = aiRoot.GetProperty("detectedFlaw").GetString();
                string reason = aiRoot.GetProperty("reason").GetString();
                string advice = aiRoot.GetProperty("advice").GetString();

                // Lưu DB
                var resultRecord = new UserCampaignResult
                {
                    UserId = claim.Id.ToString(),
                    CampaignId = request.CampaignId,
                    UserAction = request.UserAction,
                    IsCorrect = isCorrect,
                    DetectedFlaw = detectedFlaw,
                    Reason = reason,
                    Advice = advice,
                    SubmittedAt = DateTime.Now
                };

                _context.UserCampaignResults.Add(resultRecord);
                await _context.SaveChangesAsync();

                // Trả về object rõ ràng thay vì deserialize lại chuỗi
                response.SetOk(new
                {
                    isCorrect = isCorrect,
                    detectedFlaw = detectedFlaw,
                    reason = reason,
                    advice = advice
                });
                return Ok(response);
            }
            catch (ArgumentNullException)
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để làm bài." });
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

    } 
}     