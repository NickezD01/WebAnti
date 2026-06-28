using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.LessonRequest
{
    public class SetPrerequisitesRequest
    {
        public int CampaignId { get; set; }
        public List<int> LessonIds { get; set; } = new List<int>();
    }
}
