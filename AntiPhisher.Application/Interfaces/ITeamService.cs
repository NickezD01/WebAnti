using AntiPhisher.Application.Request.Team;
using AntiPhisher.Application.Request.Team.AntiPhisher.Application.Request.Team;
using AntiPhisher.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ITeamService
    {
        // CRUD Team
        Task<ApiResponse> GetAllTeamsAsync(int companyId);
        Task<ApiResponse> CreateTeamAsync(CreateTeamRequest request, int managerId, int companyId);
        Task<ApiResponse> UpdateTeamAsync(UpdateTeamRequest request);
        Task<ApiResponse> DeleteTeamAsync(int teamId);

        // Member Management
        Task<ApiResponse> AddMemberToTeamAsync(AddMemberToTeamRequest request);
        Task<ApiResponse> RemoveMemberFromTeamAsync(int teamId, int userId);
    }
}
