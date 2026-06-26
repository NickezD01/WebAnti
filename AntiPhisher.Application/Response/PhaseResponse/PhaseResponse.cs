using System.Collections.Generic;

namespace AntiPhisher.Application.Response.PhaseResponse
{
    public class PhaseDetailResponse
    {
        public int    PhaseId     { get; set; }
        public string PhaseName   { get; set; } = null!;
        public string? Description { get; set; }
        public int    OrderIndex  { get; set; }
        public bool   IsActive    { get; set; }
        public string? Color      { get; set; }
        public string? Icon       { get; set; }
        public int    ModuleCount { get; set; }
        public int    LessonCount { get; set; }
        public List<ModuleSummary> Modules { get; set; } = new();
    }

    public class ModuleSummary
    {
        public int    ModuleId   { get; set; }
        public string ModuleName { get; set; } = null!;
        public int    OrderIndex { get; set; }
        public bool   IsActive   { get; set; }
        public int    LessonCount { get; set; }
        public List<LessonSummary> Lessons { get; set; } = new();
    }

    public class LessonSummary
    {
        public int    LessonId   { get; set; }
        public string Title      { get; set; } = null!;
        public int    OrderIndex { get; set; }
        public bool   IsActive   { get; set; }
    }
}
