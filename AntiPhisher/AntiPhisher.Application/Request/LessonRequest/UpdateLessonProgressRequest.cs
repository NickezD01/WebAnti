using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Request.LessonRequest
{
    public class UpdateLessonProgressRequest
    {
        public int LessonId { get; set; }
        public bool IsCompleted { get; set; }
    }
}
