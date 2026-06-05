using AntiPhisher.Application.Request.AttemptRequest;
using AntiPhisher.Application.Request.ScenarioRequest;
using AntiPhisher.Application.Response.AttemptRespond;
using AntiPhisher.Application.Response.ScenarioRespond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IScenarioService
    {
        Task<AttemptResultResponse> SubmitAttemptAsync(SubmitAttemptRequest request, int userId);
        Task<IEnumerable<UserCampaignAttemptResponse>> GetUserCampaignAttemptsAsync(int userId, int campaignId);

        // Các hàm CRUD mới thêm
        Task<IEnumerable<ScenarioDetailResponse>> GetAllScenariosAsync();
        Task<ScenarioDetailResponse?> GetScenarioByIdAsync(int id);
        Task<ScenarioDetailResponse> CreateScenarioAsync(CreateScenarioRequest request);
        Task<ScenarioDetailResponse> UpdateScenarioAsync(int id, UpdateScenarioRequest request);
        Task<bool> DeleteScenarioAsync(int id);
    }
}
