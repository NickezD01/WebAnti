using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.LessonResponse
{
    public class UserLessonProgressResponse
    {
        public int ProgressId { get; set; }
        public int UserId { get; set; }
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = null!;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
