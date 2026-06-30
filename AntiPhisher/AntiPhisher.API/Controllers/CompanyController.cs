using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CompanyRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        /// <summary>
        /// Lấy thông tin công ty của user đang đăng nhập.
        /// GET /api/Company/my-company
        /// </summary>
        [Authorize]
        [HttpGet("my-company")]
        public async Task<IActionResult> GetMyCompany()
        {
            var response = await _companyService.GetMyCompanyAsync();
            return Ok(response);
        }

        /// <summary>
        /// Manager gửi lời mời tới email nhân viên (email đã có hoặc chưa có trong hệ thống).
        /// POST /api/Company/invite-employee
        /// </summary>
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost("invite-employee")]
        public async Task<IActionResult> InviteEmployee([FromBody] InviteByEmailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _companyService.InviteEmployeeAsync(dto);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Nhân viên xác nhận lời mời bằng token trong email.
        /// POST /api/Company/accept-invite
        /// </summary>
        [HttpPost("accept-invite")]
        public async Task<IActionResult> AcceptInvite([FromQuery] string token)
        {
            var response = await _companyService.AcceptInvitationAsync(token);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Cập nhật tên công ty — cho phép bất kỳ user nào đang sở hữu công ty (kể cả trước khi role upgrade).
        /// PUT /api/Company/update-name
        /// </summary>
        [Authorize]
        [HttpPut("update-name")]
        public async Task<IActionResult> UpdateCompanyName([FromBody] UpdateCompanyNameDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _companyService.UpdateCompanyNameAsync(dto.CompanyName);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}
