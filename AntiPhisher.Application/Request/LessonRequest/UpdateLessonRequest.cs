namespace AntiPhisher.Application.Request.LessonRequest
{
    public class UpdateLessonRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }         // maps to TheoryContent
        public string? SimulationGuide { get; set; }
    }
}
