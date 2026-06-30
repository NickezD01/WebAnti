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
    public class TacticStat
    {
        public string CategoryName { get; set; }
        public int FlawCount { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class UserFlawSummaryService : IUserFlawSummaryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOpenRouterAnalysisService _aiService;

        private static readonly TimeSpan CacheValidDuration = TimeSpan.FromHours(24);

        private const string DefaultNoDataAdviceJson =
            "{\"riskPattern\": \"Chưa có dữ liệu\", \"primaryWeakness\": \"Chưa xác định\", " +
            "\"confidenceScore\": 0, \"actionableAdvice\": [\"Hãy hoàn thành vài bài mô phỏng để nhận phân tích cá nhân hóa.\"], " +
            "\"recommendedNextDifficulty\": \"Easy\"}";

        public UserFlawSummaryService(IUnitOfWork unitOfWork, IOpenRouterAnalysisService aiService)
        {
            _unitOfWork = unitOfWork;
            _aiService = aiService;
        }

        // =========================================================
        // BƯỚC 2.1 + 2.6 — Tính toán thống kê thuần (KHÔNG gọi AI ở đây)
        // =========================================================
        private async Task<(List<TacticStat> tactics, int totalAttempts, int totalFlaws, decimal riskScore)>
            ComputeFlawStatsAsync(int userId, int lookbackDays = 90)
        {
            var cutoff = DateTime.UtcNow.AddDays(-lookbackDays);

            var attempts = await _unitOfWork.UserAttempts.GetAllAsync(
                filter: a => a.UserId == userId && a.SubmittedAt >= cutoff,
                include: q => q.Include(a => a.Scenario).ThenInclude(s => s.Category)
            );

            var attemptList = attempts?.ToList() ?? new List<UserAttempt>();
            int totalAttempts = attemptList.Count;

            var flaws = attemptList.Where(a => !a.IsCorrect).ToList();
            int totalFlaws = flaws.Count;

            var tacticStats = flaws
                .Where(a => a.Scenario?.Category != null)
                .GroupBy(a => a.Scenario.Category.CategoryName)
                .Select(g => new TacticStat
                {
                    CategoryName = g.Key,
                    FlawCount = g.Count(),
                    LastSeen = g.Max(a => a.SubmittedAt)
                })
                .OrderByDescending(t => t.FlawCount)
                .Take(5)
                .ToList();

            decimal riskScore = totalAttempts > 0
                ? Math.Round((decimal)(totalAttempts - totalFlaws) / totalAttempts * 100, 2)
                : 0m;

            return (tacticStats, totalAttempts, totalFlaws, riskScore);
        }

        // =========================================================
        // BƯỚC 2.3 + 2.6 — Orchestrate: tính toán → check cache → gọi AI → lưu cache
        // =========================================================
        public async Task<string> RefreshAndGetAdviceAsync(int userId, bool forceRefresh = false)
        {
            var existingSummary = await _unitOfWork.UserFlawSummaries.GetAsync(
                filter: s => s.UserId == userId);

            bool cacheStillValid = existingSummary?.LastAnalyzedAt != null
                && DateTime.UtcNow - existingSummary.LastAnalyzedAt.Value < CacheValidDuration;

            if (!forceRefresh && cacheStillValid && !string.IsNullOrEmpty(existingSummary.LastAiAdviceJson))
            {
                return existingSummary.LastAiAdviceJson;
            }

            var (tactics, totalAttempts, totalFlaws, riskScore) = await ComputeFlawStatsAsync(userId);

            if (totalAttempts == 0)
            {
                await UpsertSummaryAsync(userId, existingSummary, tacticsJson: "[]",
                    totalAttempts: 0, totalFlaws: 0, riskScore: 0m,
                    aiAdviceJson: DefaultNoDataAdviceJson);

                return DefaultNoDataAdviceJson;
            }

            var tacticStatsJson = JsonSerializer.Serialize(tactics);

            var aiAdviceJson = await _aiService.GenerateUserPredictiveAdviceAsync(
                tacticStatsJson, totalAttempts, totalFlaws);

            await UpsertSummaryAsync(userId, existingSummary, tacticStatsJson,
                totalAttempts, totalFlaws, riskScore, aiAdviceJson);

            return aiAdviceJson;
        }

        // === BƯỚC 2.6: tách logic lưu DB thành helper riêng, tránh lặp code ===
        private async Task UpsertSummaryAsync(
            int userId,
            UserFlawSummary existingSummary,
            string tacticsJson,
            int totalAttempts,
            int totalFlaws,
            decimal riskScore,
            string aiAdviceJson)
        {
            if (existingSummary == null)
            {
                var newSummary = new UserFlawSummary
                {
                    UserId = userId,
                    TopTacticsJson = tacticsJson,
                    TotalAttempts = totalAttempts,
                    TotalFlaws = totalFlaws,
                    CurrentRiskScore = riskScore,
                    LastAiAdviceJson = aiAdviceJson,
                    LastAnalyzedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.UserFlawSummaries.AddAsync(newSummary);
            }
            else
            {
                existingSummary.TopTacticsJson = tacticsJson;
                existingSummary.TotalAttempts = totalAttempts;
                existingSummary.TotalFlaws = totalFlaws;
                existingSummary.CurrentRiskScore = riskScore;
                existingSummary.LastAiAdviceJson = aiAdviceJson;
                existingSummary.LastAnalyzedAt = DateTime.UtcNow;
                existingSummary.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangeAsync();
        }

        // =========================================================
        // BƯỚC 2.4 — Parse JSON advice thành DTO có cấu trúc
        // =========================================================
        public async Task<Response.UserFlawSummary.UserPredictiveAdviceResponse> GetAdviceAsync(
            int userId, bool forceRefresh = false)
        {
            var rawJson = await RefreshAndGetAdviceAsync(userId, forceRefresh);

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<Response.UserFlawSummary.UserPredictiveAdviceResponse>(rawJson, options);
                return result ?? new Response.UserFlawSummary.UserPredictiveAdviceResponse
                {
                    RiskPattern = "Không thể phân tích dữ liệu lúc này.",
                    PrimaryWeakness = "Không xác định",
                    ActionableAdvice = new List<string> { "Vui lòng thử lại sau." },
                    RecommendedNextDifficulty = "Medium"
                };
            }
            catch (JsonException)
            {
                return new Response.UserFlawSummary.UserPredictiveAdviceResponse
                {
                    RiskPattern = "Không thể phân tích dữ liệu lúc này.",
                    PrimaryWeakness = "Không xác định",
                    ActionableAdvice = new List<string> { "Hệ thống AI trả về dữ liệu không hợp lệ, vui lòng thử lại." },
                    RecommendedNextDifficulty = "Medium"
                };
            }
        }
    }
}