namespace AntiPhisher.Application.Response.SubscriptionPlan
{
    public class SubscriptionPlanResponse
    {
        public int Id { get; set; }

        // CHANGED: Từ SubscriptionPlanName (enum) → string linh hoạt
        public string Name { get; set; } = string.Empty;

        public decimal? Price { get; set; }
        public int DurationInMonths { get; set; }
        public string? Description { get; set; }
        public string? Feature { get; set; }
        public bool IsActive { get; set; }
        public int MaxSlots { get; set; }
        public int ActiveSubscribersCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
