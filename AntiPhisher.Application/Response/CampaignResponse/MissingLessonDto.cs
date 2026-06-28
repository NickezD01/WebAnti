using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.CampaignResponse
{
    public class MissingLessonDto
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public string ModuleName { get; set; } = null!;
        public string PhaseName { get; set; } = null!;

        // Các trường hỗ trợ Frontend nếu cần xử lý sắp xếp hoặc hiển thị badge số thứ tự
        public int PhaseOrder { get; set; }
        public int ModuleOrder { get; set; }
        public int LessonOrder { get; set; }
    }
}
