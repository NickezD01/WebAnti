using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.CampaignRequest
{
    public class UpdateCampaignRequest
    {
        public string CampaignName { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsActive { get; set; }

        // Danh sách cập nhật mới (sẽ ghi đè hoặc đồng bộ lại các quan hệ cũ)
        public List<int> ScenarioIds { get; set; } = new List<int>();
        public List<int>? TeamIds { get; set; }
        public List<int>? UserIds { get; set; }
    }
}
