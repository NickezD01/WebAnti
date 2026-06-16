namespace AntiPhisher.Application.Response.Analytics
{
    public class AdminOverviewResponse
    {
        // ── KPIs ──────────────────────────────────────────────
        public long TotalRevenue { get; set; }
        public long RevenueThisMonth { get; set; }
        public long RevenueLastMonth { get; set; }
        public int TotalTransactions { get; set; }
        public int PaidTransactions { get; set; }
        public int PendingTransactions { get; set; }
        public int TotalUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveSubscriptions { get; set; }
        public int TotalCompanies { get; set; }

        // ── Charts ────────────────────────────────────────────
        public List<MonthlyStatItem> RevenueByMonth { get; set; } = new();
        public List<MonthlyStatItem> UserGrowthByMonth { get; set; } = new();
        public List<PlanBreakdownItem> PlanBreakdown { get; set; } = new();

        // ── Recent orders ─────────────────────────────────────
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public class MonthlyStatItem
    {
        public string Month { get; set; } = string.Empty;
        public long Value { get; set; }
    }

    public class PlanBreakdownItem
    {
        public string PlanName { get; set; } = string.Empty;
        public int Count { get; set; }
        public long Revenue { get; set; }
    }

    public class RecentOrderItem
    {
        public int OrderId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string PlanName { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
