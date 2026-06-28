using AntiPhisher.Application.Interfaces;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class OrgTacticStat
    {
        public string CategoryName { get; set; }
        public int TotalExposed { get; set; }   // số lần Scenario thuộc category này được làm
        public int TotalFailed { get; set; }     // số lần fail (IsCorrect == false)
        public decimal FailRate { get; set; }    // TotalFailed / TotalExposed * 100
    }

    public class HighRiskUserStat
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public decimal RiskScore { get; set; }   // lấy từ UserFlawSummary.CurrentRiskScore nếu có
        public int TotalFlaws { get; set; }
    }

    public class OrgFlawStats
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalFlaws { get; set; }
        public decimal OrgFailRate { get; set; }
        public List<OrgTacticStat> TacticBreakdown { get; set; } = new();
        public List<HighRiskUserStat> HighRiskUsers { get; set; } = new();
    }

    // === MỚI: DTO khớp schema JSON từ GenerateExecutiveSummaryAsync ===
    public class ExecutiveSummaryDto
    {
        public string ExecutiveSummary { get; set; }
        public List<string> TopRisks { get; set; } = new();
        public string SuggestedNextCampaignContext { get; set; }
        public string SuggestedDifficulty { get; set; }
    }

    // === MỚI: Kết quả tổng hợp trả ra Controller ===
    public class OrgReportResult
    {
        public OrgFlawStats Stats { get; set; }
        public ExecutiveSummaryDto AiSummary { get; set; }
    }

    public class OrgReportService : IOrgReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOpenRouterAnalysisService _aiService;

        public OrgReportService(IUnitOfWork unitOfWork, IOpenRouterAnalysisService aiService)
        {
            _unitOfWork = unitOfWork;
            _aiService = aiService;
        }

        // =========================================================
        // BƯỚC 3.1 — Tính toán thuần (KHÔNG gọi AI)
        // =========================================================
        public async Task<OrgFlawStats> ComputeOrgStatsAsync(int companyId, int lookbackDays = 90)
        {
            var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == companyId);
            if (company == null)
                throw new Exception($"Không tìm thấy công ty với ID {companyId}.");

            // 1. Lấy toàn bộ user thuộc company này
            var employees = await _unitOfWork.Users.GetAllAsync(u => u.CompanyId == companyId);
            var employeeList = employees?.ToList() ?? new List<User>();
            var employeeIds = employeeList.Select(e => e.UserId).ToHashSet();

            int totalEmployees = employeeList.Count;

            if (totalEmployees == 0 || employeeIds.Count == 0)
            {
                return new OrgFlawStats
                {
                    CompanyId = companyId,
                    CompanyName = company.CompanyName,
                    TotalEmployees = 0
                };
            }

            // 2. Lấy toàn bộ attempt của các user này trong khoảng thời gian, kèm Scenario.Category
            var cutoff = DateTime.UtcNow.AddDays(-lookbackDays);
            var attempts = await _unitOfWork.UserAttempts.GetAllAsync(
                filter: a => employeeIds.Contains(a.UserId) && a.SubmittedAt >= cutoff,
                include: q => q.Include(a => a.Scenario).ThenInclude(s => s.Category)
            );
            var attemptList = attempts?.ToList() ?? new List<UserAttempt>();

            int totalAttempts = attemptList.Count;
            int totalFlaws = attemptList.Count(a => !a.IsCorrect);
            decimal orgFailRate = totalAttempts > 0
                ? Math.Round((decimal)totalFlaws / totalAttempts * 100, 2)
                : 0m;

            // 3. Tactic breakdown — group theo Category
            var tacticBreakdown = attemptList
                .Where(a => a.Scenario?.Category != null)
                .GroupBy(a => a.Scenario.Category.CategoryName)
                .Select(g => new OrgTacticStat
                {
                    CategoryName = g.Key,
                    TotalExposed = g.Count(),
                    TotalFailed = g.Count(a => !a.IsCorrect),
                    FailRate = g.Count() > 0
                        ? Math.Round((decimal)g.Count(a => !a.IsCorrect) / g.Count() * 100, 2)
                        : 0m
                })
                .OrderByDescending(t => t.FailRate)
                .ToList();

            // 4. High-risk users — lấy từ UserFlawSummary đã cache (Giai đoạn 2)
            var summaries = await _unitOfWork.UserFlawSummaries.GetAllAsync(
                s => employeeIds.Contains(s.UserId));
            var summaryList = summaries?.ToList() ?? new List<UserFlawSummary>();

            var highRiskUsers = summaryList
                .Where(s => s.TotalAttempts > 0)
                .OrderBy(s => s.CurrentRiskScore) // risk score THẤP = rủi ro CAO
                .Take(10)
                .Select(s =>
                {
                    var emp = employeeList.FirstOrDefault(e => e.UserId == s.UserId);
                    return new HighRiskUserStat
                    {
                        UserId = s.UserId,
                        FullName = emp?.FullName ?? "(không xác định)",
                        RiskScore = s.CurrentRiskScore,
                        TotalFlaws = s.TotalFlaws
                    };
                })
                .ToList();

            return new OrgFlawStats
            {
                CompanyId = companyId,
                CompanyName = company.CompanyName,
                TotalEmployees = totalEmployees,
                TotalAttempts = totalAttempts,
                TotalFlaws = totalFlaws,
                OrgFailRate = orgFailRate,
                TacticBreakdown = tacticBreakdown,
                HighRiskUsers = highRiskUsers
            };
        }

        // =========================================================
        // BƯỚC 3.3 — Orchestrate: tính toán → gọi AI → parse → trả về
        // (On-demand, CHƯA cache vào bảng riêng — đơn giản hóa cho đồ án)
        // =========================================================
        public async Task<OrgReportResult> GetOrgReportAsync(int companyId, bool forceRefresh = false)
        {
            var stats = await ComputeOrgStatsAsync(companyId);

            // Nếu công ty chưa có nhân viên hoặc chưa có dữ liệu → không gọi AI, tránh lãng phí
            if (stats.TotalEmployees == 0 || stats.TotalAttempts == 0)
            {
                return new OrgReportResult
                {
                    Stats = stats,
                    AiSummary = new ExecutiveSummaryDto
                    {
                        ExecutiveSummary = "Chưa có đủ dữ liệu để phân tích cho công ty này.",
                        TopRisks = new List<string>(),
                        SuggestedNextCampaignContext = "",
                        SuggestedDifficulty = "Easy"
                    }
                };
            }

            var tacticJson = JsonSerializer.Serialize(stats.TacticBreakdown);

            var aiRawJson = await _aiService.GenerateExecutiveSummaryAsync(
                stats.CompanyName, stats.TotalEmployees, stats.OrgFailRate, tacticJson);

            ExecutiveSummaryDto aiSummary;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                aiSummary = JsonSerializer.Deserialize<ExecutiveSummaryDto>(aiRawJson, options)
                    ?? new ExecutiveSummaryDto { ExecutiveSummary = "Không thể phân tích dữ liệu lúc này." };
            }
            catch (JsonException)
            {
                aiSummary = new ExecutiveSummaryDto
                {
                    ExecutiveSummary = "Hệ thống AI trả về dữ liệu không hợp lệ, vui lòng thử lại.",
                    TopRisks = new List<string>(),
                    SuggestedNextCampaignContext = "",
                    SuggestedDifficulty = "Medium"
                };
            }

            return new OrgReportResult
            {
                Stats = stats,
                AiSummary = aiSummary
            };
        }
    }
}