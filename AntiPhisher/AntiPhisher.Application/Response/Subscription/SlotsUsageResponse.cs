namespace AntiPhisher.Application.Response.Subscription
{
    public class SlotsUsageResponse
    {
        public int UsedSlots { get; set; }
        public int TotalSlots { get; set; }
        public int RemainingSlots { get; set; }
        public string PlanName { get; set; } = string.Empty;
    }
}
