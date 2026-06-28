namespace AntiPhisher.Application.Request.AIKnowledge
{
    public class CreateAIKnowledgeRequest
    {
        public string Title { get; set; }
        public string ContextDescription { get; set; }
        public string SourceType { get; set; } = "AdminManual";
        public string SourceUrl { get; set; }
        public string SampleEmailContent { get; set; }
        public string Tags { get; set; }
        public int DifficultyId { get; set; }
    }
}