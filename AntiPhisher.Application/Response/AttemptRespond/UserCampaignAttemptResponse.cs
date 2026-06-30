namespace AntiPhisher.Application.Response.AttemptRespond
{
    public class UserCampaignAttemptResponse
    {
        public int AttemptId { get; set; }
        public int ScenarioId { get; set; }
        public bool IsCorrect { get; set; }
        public int Score { get; set; }
        public bool IsClickedLink { get; set; }
        public bool IsCredentialLeaked { get; set; }
        public bool IsReported { get; set; }
        public DateTime SubmittedAt { get; set; }
    }
}
