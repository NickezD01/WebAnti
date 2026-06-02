using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.AttemptRequest;
using AntiPhisher.Application.Request.ScenarioRequest; // Mở Khóa Namespace DTO mới
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.ScenarioRespond;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace AntiPhisher.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScenariosController : ControllerBase
    {
        private readonly IScenarioService _scenarioService;

        public ScenariosController(IScenarioService scenarioService)
        {
            _scenarioService = scenarioService;
        }

        #region ================= USER ENDPOINTS =================

        [HttpGet]
        public async Task<IActionResult> GetAllScenarios()
        {
            var response = new ApiResponse();
            try
            {
                var scenarios = await _scenarioService.GetAllScenariosAsync();
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
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                response.SetApiResponse(HttpStatusCode.Unauthorized, false, "Xác thực tài khoản thất bại.");
                return Unauthorized(response);
            }

            if (!int.TryParse(userIdClaim, out int userId))
            {
                response.SetBadRequest(message: "Định dạng định danh người dùng không hợp lệ.");
                return BadRequest(response);
            }

            try
            {
                var result = await _scenarioService.SubmitAttemptAsync(request, userId);
                response.SetOk(result);
                return Ok(response);
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