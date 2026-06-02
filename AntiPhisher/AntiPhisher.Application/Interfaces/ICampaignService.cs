using AntiPhisher.Application.Request.CampaignRequest;
using AntiPhisher.Application.Response.CampaignResponse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ICampaignService
    {
        Task<IEnumerable<CampaignDetailResponse>> GetAllCampaignsAsync();
        Task<CampaignDetailResponse?> GetCampaignByIdAsync(int id);
        Task<CampaignDetailResponse> CreateCampaignAsync(CreateCampaignRequest request, int callerId, string callerRole);
        Task<bool> DeleteCampaignAsync(int id, int callerId, string callerRole);

        /// <summary>
        /// Toggle IsActive. Khi false→true (activate): sinh UserLessonProgress cho tất cả user được assign.
        /// Khi true→false (deactivate): chỉ set cờ, không xóa progress.
        /// Admin thao tác bất kỳ; Manager chỉ campaign thuộc công ty mình.
        /// </summary>
        Task<CampaignDetailResponse> UpdateCampaignStatusAsync(int campaignId, bool isActive, int callerId, string callerRole);
    }
}
