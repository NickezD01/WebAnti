using System;

namespace AntiPhisher.Application.Response.AIKnowledge
{
    public class AIKnowledgeResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ContextDescription { get; set; }
        public string Tags { get; set; }
        public int DifficultyId { get; set; }
        public string DifficultyName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}