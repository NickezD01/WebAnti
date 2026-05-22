using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CampaignRequest;
using AntiPhisher.Application.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CampaignsController : ControllerBase
    {
        private readonly ICampaignService _campaignService;

        public CampaignsController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
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

        [HttpPost]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            var response = new ApiResponse();
            var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out int adminId))
            {
                response.SetBadRequest(message: "Không thể xác thực danh tính tài khoản quản trị viên.");
                return BadRequest(response);
            }

            try
            {
                var createdCampaign = await _campaignService.CreateCampaignAsync(request, adminId);
                response.SetOk(createdCampaign);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var response = new ApiResponse();
            try
            {
                await _campaignService.DeleteCampaignAsync(id);
                response.SetOk(new { Message = $"Đã xóa thành công chiến dịch ID {id}" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }
    }
}