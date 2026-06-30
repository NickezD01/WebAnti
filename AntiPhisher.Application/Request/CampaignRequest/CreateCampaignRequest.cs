using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.CampaignRequest
{
    public class CreateCampaignRequest
    {
        public string CampaignName { get; set; } = null!;
        public string? Description { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int? CompanyId { get; set; }

        // Danh sách ID kịch bản muốn đưa vào chiến dịch
        public List<int> ScenarioIds { get; set; } = new List<int>();

        // Danh sách ID đội ngũ hoặc Cá nhân được gán tham gia
        public List<int>? TeamIds { get; set; }
        public List<int>? UserIds { get; set; }


        public List<int> RequiredLessonIds { get; set; } = new List<int>();
    }
}
