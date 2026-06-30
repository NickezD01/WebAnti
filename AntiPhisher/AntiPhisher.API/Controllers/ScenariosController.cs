using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.AttemptRequest;
using AntiPhisher.Application.Request.ScenarioRequest;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.ScenarioRespond;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScenariosController : ControllerBase
    {
        private readonly IScenarioService _scenarioService;
        private readonly IClaimService _claimService;

        public ScenariosController(IScenarioService scenarioService, IClaimService claimService)
        {
            _scenarioService = scenarioService;
            _claimService = claimService;
        }

        #region ================= USER ENDPOINTS =================

        [HttpGet]
        public async Task<IActionResult> GetAllScenarios()
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var scenarios = await _scenarioService.GetScenariosForUserAsync(claim.Id);
                response.SetOk(scenarios);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetScenarioById(int id)
        {
            var response = new ApiResponse();
            try
            {
                var scenario = await _scenarioService.GetScenarioByIdAsync(id);
                if (scenario == null)
                {
                    response.SetNotFound(message: $"Không tìm thấy kịch bản với ID {id}");
                    return NotFound(response);
                }
                response.SetOk(scenario);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        [HttpPost("submit-attempt")]
        public async Task<IActionResult> SubmitAttempt([FromBody] SubmitAttemptRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var result = await _scenarioService.SubmitAttemptAsync(request, claim.Id);
                response.SetOk(result);
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

        // GET api/Scenarios/my-campaign-attempts?campaignId=1
        [HttpGet("my-campaign-attempts")]
        public async Task<IActionResult> GetMyCampaignAttempts([FromQuery] int campaignId)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var attempts = await _scenarioService.GetUserCampaignAttemptsAsync(claim.Id, campaignId);
                response.SetOk(attempts);
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

        #endregion

        #region ================= MANAGER ENDPOINTS =================

        [HttpPost("company")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> CreateCompanyScenario([FromBody] CreateScenarioRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var claim = _claimService.GetUserClaim();
                var result = await _scenarioService.CreateCompanyScenarioAsync(request, claim.Id);
                response.SetApiResponse(HttpStatusCode.Created, true, "Tạo kịch bản công ty thành công!", result);
                return CreatedAtAction(nameof(GetScenarioById), new { id = result.ScenarioId }, response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        #endregion

        #region ================= ADMIN ENDPOINTS =================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateScenario([FromBody] CreateScenarioRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var createdScenario = await _scenarioService.CreateScenarioAsync(request);
                response.SetApiResponse(HttpStatusCode.Created, true, "Tạo kịch bản thành công!", createdScenario);
                return CreatedAtAction(nameof(GetScenarioById), new { id = createdScenario.ScenarioId }, response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateScenario(int id, [FromBody] UpdateScenarioRequest request)
        {
            var response = new ApiResponse();
            try
            {
                var updatedScenario = await _scenarioService.UpdateScenarioAsync(id, request);
                response.SetOk(updatedScenario);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteScenario(int id)
        {
            var response = new ApiResponse();
            try
            {
                await _scenarioService.DeleteScenarioAsync(id);
                response.SetOk(new { Message = $"Đã xóa thành công kịch bản ID {id}" });
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.SetBadRequest(message: ex.Message);
                return BadRequest(response);
            }
        }

        #endregion
    }
}
