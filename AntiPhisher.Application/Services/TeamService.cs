using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.Team;
using AntiPhisher.Application.Request.Team.AntiPhisher.Application.Request.Team;
using AntiPhisher.Application.Response;
using AntiPhisher.Application.Response.TeamResponse;
using AntiPhisher.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AntiPhisher.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TeamService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<ApiResponse> GetAllTeamsAsync(int companyId)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // 1. Dùng Include để lấy dữ liệu liên quan (Ví dụ: lấy luôn danh sách thành viên)
                var teams = await _unitOfWork.Teams.GetAllAsync(
                    t => t.CompanyId == companyId && t.IsActive,
                    include: source => source.Include(t => t.TeamMembers).ThenInclude(tm => tm.User)
                );

                // 2. Map sang DTO để trả về đúng định dạng frontend cần
                var teamResponses = _mapper.Map<List<TeamResponse>>(teams);

                return apiResponse.SetOk(teamResponses);
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest(ex.Message);
            }
        }

        public async Task<ApiResponse> CreateTeamAsync(CreateTeamRequest request, int managerId, int companyId)
        {
            var team = new Team
            {
                TeamName = request.TeamName,
                Description = request.Description,
                ManagerId = managerId,
                CompanyId = companyId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Teams.AddAsync(team);
            await _unitOfWork.SaveChangeAsync();
            return new ApiResponse().SetOk("Tạo nhóm thành công.");
        }

        public async Task<ApiResponse> UpdateTeamAsync(UpdateTeamRequest request)
        {
            var team = await _unitOfWork.Teams.GetAsync(t => t.TeamId == request.TeamId);
            if (team == null) return new ApiResponse().SetBadRequest("Nhóm không tồn tại.");

            team.TeamName = request.TeamName;
            team.Description = request.Description;
            team.IsActive = request.IsActive;
            team.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();
            return new ApiResponse().SetOk("Cập nhật nhóm thành công.");
        }

        public async Task<ApiResponse> DeleteTeamAsync(int teamId)
        {
            var team = await _unitOfWork.Teams.GetAsync(t => t.TeamId == teamId);
            if (team == null) return new ApiResponse().SetBadRequest("Nhóm không tồn tại.");

            // Soft delete bằng cách set IsActive = false
            team.IsActive = false;
            await _unitOfWork.SaveChangeAsync();
            return new ApiResponse().SetOk("Đã xóa nhóm.");
        }
        public async Task<ApiResponse> RemoveMemberFromTeamAsync(int teamId, int userId)
        {
            var apiResponse = new ApiResponse();
            try
            {
                // 1. Tìm thành viên trong nhóm
                var member = await _unitOfWork.TeamMembers.GetAsync(x => x.TeamId == teamId && x.UserId == userId);
                if (member == null)
                    return apiResponse.SetBadRequest("Thành viên không tồn tại trong nhóm này.");

                // 2. Xóa thành viên
                _unitOfWork.TeamMembers.Remove(member);
                await _unitOfWork.SaveChangeAsync();

                return apiResponse.SetOk("Đã xóa thành viên khỏi nhóm.");
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Lỗi hệ thống: {ex.Message}");
            }
        }

        public async Task<ApiResponse> AddMemberToTeamAsync(AddMemberToTeamRequest request)
        {
            ApiResponse apiResponse = new ApiResponse();
            try
            {
                // 1. Kiểm tra User có tồn tại không
                var user = await _unitOfWork.Users.GetAsync(x => x.UserId == request.UserId);
                if (user == null)
                    return apiResponse.SetBadRequest("Người dùng không tồn tại.");

                // 2. Kiểm tra Team có tồn tại không
                var team = await _unitOfWork.Teams.GetAsync(x => x.TeamId == request.TeamId);
                if (team == null)
                    return apiResponse.SetBadRequest("Nhóm không tồn tại.");

                // 3. Kiểm tra User đã nằm trong Team đó chưa
                var isAlreadyMember = await _unitOfWork.TeamMembers.GetAsync(x => x.TeamId == request.TeamId && x.UserId == request.UserId);
                if (isAlreadyMember != null)
                    return apiResponse.SetBadRequest("Người dùng đã thuộc nhóm này rồi.");

                // 4. Thêm thành viên mới
                var newMember = new TeamMember
                {
                    TeamId = request.TeamId,
                    UserId = request.UserId,
                    // Nếu có thêm thuộc tính như JoinedAt, hãy gán tại đây
                };

                await _unitOfWork.TeamMembers.AddAsync(newMember);
                await _unitOfWork.SaveChangeAsync();

                return apiResponse.SetOk("Thêm thành viên vào nhóm thành công.");
            }
            catch (Exception ex)
            {
                return apiResponse.SetBadRequest($"Lỗi hệ thống: {ex.Message}");
            }
        }
    }
}