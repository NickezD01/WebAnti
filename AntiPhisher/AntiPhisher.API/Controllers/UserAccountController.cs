using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.User;
using AntiPhisher.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : ControllerBase
    {
        public IUserAccountService _service;
        private readonly IClaimService _claimService;

        public UserAccountController(IUserAccountService service, IClaimService claimService)
        {
            _service = service;
            _claimService = claimService;
        }
        [Authorize]
        [HttpGet("GetUserProfile")]
        public async Task<IActionResult> GetUserProfileAsync()
        {
            var resposne = await _service.GetUserProfileAsync();
            return resposne.IsSuccess ? Ok(resposne) : BadRequest(resposne);
        }
        [Authorize]
        [HttpPut("UpdateUserProfile")]
        public async Task<IActionResult> UpdateUserProfileAsync(UpdateUserRequest updateUserRequest)
        {
            var result = await _service.UpdateUserProfileAsync(updateUserRequest);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
        [Authorize(Roles = "Admin")]
        [HttpGet("GetAllAccountAsync")]
        public async Task<IActionResult> GetAllAccountAsync([FromQuery] string? searchTerm = "", [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
        {
            var resposne = await _service.GetAllAccountAsync(searchTerm ?? "", pageIndex, pageSize);
            return resposne.IsSuccess ? Ok(resposne) : BadRequest(resposne);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("UpdateUserStatusOrRole")]
        public async Task<IActionResult> UpdateUserStatusOrRoleAsync([FromBody] UpdateUserStatusOrRoleRequest request)
        {
            var result = await _service.UpdateUserStatusOrRoleAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        // =====================================================================
        // PHẦN 1: Danh sách nhân viên của công ty (Manager only)
        // GET: api/UserAccount/company-employees?searchTerm=&pageIndex=1&pageSize=10
        // =====================================================================

        [Authorize(Roles = "Manager")]
        [HttpGet("company-employees")]
        public async Task<IActionResult> GetCompanyEmployeesAsync(
            [FromQuery] string? searchTerm = "",
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var claim = _claimService.GetUserClaim();
            var result = await _service.GetCompanyEmployeesAsync(claim.Id, searchTerm ?? "", pageIndex, pageSize);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Manager")] // Tùy chọn: Chặn chỉ cho tài khoản có quyền Manager gọi (nếu hệ thống của bạn có phân quyền chặt)
        [HttpGet("my-manager")]
        public async Task<IActionResult> GetMyManagerAsync()
        {
            var response = await _service.GetMyManagerAsync();
            return Ok(response);
        }

        /// <summary>
        /// API dành cho Manager xem danh sách nhân viên trong NHÓM (TEAM) mình quản lý
        /// Route từ FE: GET /api/UserAccount/my-team-members
        /// </summary>
        [Authorize(Roles = "Manager")]
        [HttpGet("my-team-members")]
        public async Task<IActionResult> GetMyTeamMembers()
        {
            var response = await _service.GetMyTeamMembersAsync();
            return Ok(response);
        }

        /// <summary>
        /// API dành cho Manager xem danh sách tất cả nhân viên trong cùng CÔNG TY
        /// Route từ FE: GET /api/UserAccount/company-employees
        /// </summary>
        //[Authorize(Roles = "Manager")]
        //[HttpGet("company-employee")]
        //public async Task<IActionResult> GetCompanyEmployees()
        //{
        //    var response = await _service.GetEmployeesInCompanyAsync();
        //    return Ok(response);
        //}
    }
}
