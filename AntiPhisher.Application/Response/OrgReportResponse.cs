using System.Collections.Generic;

namespace AntiPhisher.Application.Response.OrgReport
{
    public class OrgTacticStatResponse
    {
        public string CategoryName { get; set; }
        public int TotalExposed { get; set; }
        public int TotalFailed { get; set; }
        public decimal FailRate { get; set; }
    }

    public class HighRiskUserResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public decimal RiskScore { get; set; }
        public int TotalFlaws { get; set; }
    }

    public class OrgReportResponse
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalFlaws { get; set; }
        public decimal OrgFailRate { get; set; }
        public List<OrgTacticStatResponse> TacticBreakdown { get; set; } = new();
        public List<HighRiskUserResponse> HighRiskUsers { get; set; } = new();

        // AI Executive Summary
        public string ExecutiveSummary { get; set; }
        public List<string> TopRisks { get; set; } = new();
        public string SuggestedNextCampaignContext { get; set; }
        public string SuggestedDifficulty { get; set; }
    }
}