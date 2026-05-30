using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : ControllerBase
    {
        public IUserAccountService _service;
        public UserAccountController(IUserAccountService service)
        {
            _service = service;
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
    }
}
