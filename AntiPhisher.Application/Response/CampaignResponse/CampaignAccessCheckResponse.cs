using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.CampaignResponse
{
    public class CampaignAccessCheckResponse
    {
        public int CampaignId { get; set; }
        public string CampaignName { get; set; } = null!;
        public bool IsLocked { get; set; }
        public string Message { get; set; } = null!;
        public List<MissingLessonDto> MissingLessons { get; set; } = new List<MissingLessonDto>();
    }
}
