namespace AntiPhisher.Application.DTOs.Analytics
{
    public class LeaderboardResponse
    {
        public List<LeaderboardEntry> Entries { get; set; } = new();
    }

    public class LeaderboardEntry
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public int TotalAttempts { get; set; }
        public float CorrectRate { get; set; }
        public int Rank { get; set; }
    }
}
