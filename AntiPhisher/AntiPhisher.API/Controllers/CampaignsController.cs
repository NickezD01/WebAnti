using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CampaignRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;
        private readonly IClaimService _claimService;

        public CampaignsController(ICampaignService campaignService, IClaimService claimService)
        {
            _campaignService = campaignService;
            _claimService = claimService;
        }

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

        /// <summary>
        /// Chuyển trạng thái campaign: isActive=true (Activate) hoặc isActive=false (Tạm dừng).
        /// Khi activate (false→true): tự động sinh UserLessonProgress cho tất cả user được assign.
        /// PUT /api/Campaigns/{id}/status
        /// Body: { "isActive": true }
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateCampaignStatus(int id, [FromBody] UpdateCampaignStatusRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var updated = await _campaignService.UpdateCampaignStatusAsync(id, request.IsActive, claim.Id, claim.Role);
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
    }

    public record UpdateCampaignStatusRequest(bool IsActive);
}