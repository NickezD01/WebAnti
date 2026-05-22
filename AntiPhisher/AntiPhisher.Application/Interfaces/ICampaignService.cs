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
        Task<CampaignDetailResponse> CreateCampaignAsync(CreateCampaignRequest request, int adminId);
        Task<bool> DeleteCampaignAsync(int id);
    }
}
