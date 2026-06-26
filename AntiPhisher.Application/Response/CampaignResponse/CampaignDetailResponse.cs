using AntiPhisher.Application.Response.ScenarioRespond;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.CampaignResponse
{
    public class CampaignDetailResponse
    {
        public int CampaignId { get; set; }
        public int CreatedByUserId { get; set; }
        public int? CompanyId { get; set; }
        public string CampaignName { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        // Trả về danh sách kịch bản bài test nằm trong chiến dịch (Đã xếp theo OrderIndex)
        public List<ScenarioDetailResponse> Scenarios { get; set; } = new List<ScenarioDetailResponse>();
    }
}
