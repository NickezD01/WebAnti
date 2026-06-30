using AntiPhisher.Application;
using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Team;
using AntiPhisher.Application.Request.Team.AntiPhisher.Application.Request.Team;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AntiPhisher.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Manager")] // Chỉ Manager mới có quyền quản lý team
    public class TeamController : ControllerBase
    {
        private readonly ITeamService _teamService;
        private readonly IClaimService _claimService;
        private readonly IUnitOfWork _unitOfWork;

        public TeamController(ITeamService teamService, IClaimService claimService, IUnitOfWork unitOfWork)
        {
            _teamService = teamService;
            _claimService = claimService;
            _unitOfWork = unitOfWork;
        }

        // Lấy danh sách tất cả nhóm của công ty
        [HttpGet]
        public async Task<IActionResult> GetAllTeams()
        {
            var managerId = _claimService.GetUserClaim().Id;
            var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == managerId);

            var response = await _teamService.GetAllTeamsAsync(manager.CompanyId ?? 0);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // Tạo nhóm mới
        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var managerId = _claimService.GetUserClaim().Id;
            var manager = await _unitOfWork.Users.GetAsync(u => u.UserId == managerId);

            var response = await _teamService.CreateTeamAsync(request, managerId, manager.CompanyId ?? 0);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // Cập nhật thông tin nhóm
        [HttpPut]
        public async Task<IActionResult> UpdateTeam([FromBody] UpdateTeamRequest request)
        {
            var response = await _teamService.UpdateTeamAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // Xóa nhóm (Soft delete)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            var response = await _teamService.DeleteTeamAsync(id);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // Thêm thành viên vào nhóm
        [HttpPost("add-member")]
        public async Task<IActionResult> AddMember([FromBody] AddMemberToTeamRequest request)
        {
            var response = await _teamService.AddMemberToTeamAsync(request);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }

        // Xóa thành viên khỏi nhóm
        [HttpDelete("remove-member/{teamId}/{userId}")]
        public async Task<IActionResult> RemoveMember(int teamId, int userId)
        {
            var response = await _teamService.RemoveMemberFromTeamAsync(teamId, userId);
            return response.IsSuccess ? Ok(response) : BadRequest(response);
        }
    }
}