using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Response.LessonResponse
{
    public class LessonResponse
    {
        public int LessonId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? SimulationGuide { get; set; }
        public int PhaseNumber { get; set; }
        public string? PhaseName { get; set; }
        public int ModuleNumber { get; set; }
        public string? ModuleName { get; set; }
        public double LessonOrder { get; set; }
        public int? EstimatedMinutes { get; set; }
    }
}
