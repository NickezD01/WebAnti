namespace AntiPhisher.Application.Response.CampaignGenerator
{
    public class SavedScenarioResponse
    {
        public int ScenarioId { get; set; }
        public string Title { get; set; }
        public string GenerationStatus { get; set; }
        public int CategoryId { get; set; }
        public int DifficultyId { get; set; }
    }
}