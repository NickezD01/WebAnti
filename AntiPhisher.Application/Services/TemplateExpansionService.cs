using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.TemplateExpansion;
using AntiPhisher.Application.Response.CampaignGenerator;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class TemplateExpansionService : ITemplateExpansionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOpenRouterAnalysisService _aiService;

        public TemplateExpansionService(IUnitOfWork unitOfWork, IOpenRouterAnalysisService aiService)
        {
            _unitOfWork = unitOfWork;
            _aiService = aiService;
        }

        public async Task<ScenarioPreviewResponse> GenerateSimilarPreviewAsync(TemplateExpansionRequest request)
        {
            var difficulty = await _unitOfWork.DifficultyLevels.GetAsync(d => d.DifficultyId == request.DifficultyId);
            if (difficulty == null)
                throw new Exception($"Không tìm thấy mức độ khó với ID {request.DifficultyId}.");

            var allCandidates = await _unitOfWork.Scenarios.GetAllAsync(
                filter: s => s.DifficultyId == request.DifficultyId &&
                             (s.GenerationStatus == "Manual" || s.GenerationStatus == "Approved") &&
                             (request.CategoryId == null || s.CategoryId == request.CategoryId.Value));

            var candidateList = allCandidates?.ToList() ?? new List<Scenario>();

            if (candidateList.Count == 0)
                throw new Exception($"Không có Scenario gốc nào ở mức độ '{difficulty.LevelName}' để làm mẫu.");

            var random = new Random();
            var fewShotScenarios = candidateList
                .OrderBy(_ => random.Next())
                .Take(request.FewShotCount)
                .ToList();

            var fewShotPayload = fewShotScenarios.Select(s => new
            {
                subject = s.Subject,
                redFlags = s.PhishingIndicators,
                bodyPreview = Truncate(s.EmailBodyHtml, 500)
            });
            var fewShotJson = JsonSerializer.Serialize(fewShotPayload);

            var aiRawJson = await _aiService.GenerateSimilarScenarioAsync(difficulty.LevelName, fewShotJson);

            ScenarioPreviewResponse preview;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                preview = JsonSerializer.Deserialize<ScenarioPreviewResponse>(aiRawJson, options)
                    ?? throw new JsonException("Deserialize trả về null.");
            }
            catch (JsonException ex)
            {
                throw new Exception($"AI trả về dữ liệu không hợp lệ, vui lòng thử lại. Chi tiết: {ex.Message}");
            }

            preview.DifficultyId = request.DifficultyId;
            preview.SourceScenarioIds = fewShotScenarios.Select(s => s.ScenarioId).ToList();
            preview.SuggestedCategoryId = fewShotScenarios.First().CategoryId;

            return preview;
        }

        // =========================================================
        // BƯỚC 5.4 — Lưu preview đã Admin xác nhận thành Scenario thật
        // =========================================================
        public async Task<SavedScenarioResponse> SaveAsScenarioAsync(SaveSimilarScenarioRequest request, int createdByUserId)
        {
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryId == request.CategoryId);
            if (category == null)
                throw new Exception($"Không tìm thấy Category với ID {request.CategoryId}.");

            var difficulty = await _unitOfWork.DifficultyLevels.GetAsync(d => d.DifficultyId == request.DifficultyId);
            if (difficulty == null)
                throw new Exception($"Không tìm thấy mức độ khó với ID {request.DifficultyId}.");

            var contentHash = ComputeContentHash(request.Title, request.EmailBodyHtml);

            var isDuplicate = await _unitOfWork.Scenarios.GetAsync(
                s => s.ContentHash != null && s.ContentHash == contentHash);

            if (isDuplicate != null)
                throw new Exception("Nội dung này đã tồn tại trong hệ thống (trùng với 1 Scenario khác), vui lòng tạo lại preview.");

            var scenario = new Scenario
            {
                CategoryId = request.CategoryId,
                DifficultyId = request.DifficultyId,
                CreatedByUserId = createdByUserId,
                Title = request.Title,
                Description = request.ExplanationHint,
                IsPhishing = request.IsPhishing,
                IsAIGenerated = true,
                IsActive = false, // Admin duyệt trước khi vào pool thật — giống Giai đoạn 4
                SenderName = request.SenderName,
                SenderEmail = request.SenderEmail,
                RecipientName = request.RecipientName,
                Subject = request.Subject,
                EmailBodyHtml = request.EmailBodyHtml,
                PhishingIndicators = request.PhishingIndicators,
                ExplanationHint = request.ExplanationHint,
                GenerationStatus = "PendingReview",
                // ĐÚNG ngữ nghĩa cột này ở Giai đoạn 5: danh sách ScenarioId gốc dùng làm few-shot
                SourceScenarioIds = string.Join(",", request.SourceScenarioIds),
                ContentHash = contentHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Scenarios.AddAsync(scenario);
            await _unitOfWork.SaveChangeAsync();

            return new SavedScenarioResponse
            {
                ScenarioId = scenario.ScenarioId,
                Title = scenario.Title,
                GenerationStatus = scenario.GenerationStatus,
                CategoryId = scenario.CategoryId,
                DifficultyId = scenario.DifficultyId
            };
        }

        private static byte[] ComputeContentHash(string title, string emailBodyHtml)
        {
            var normalized = (title + emailBodyHtml).ToLowerInvariant().Trim();
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        }

        private static string Truncate(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                return input;
            return input.Substring(0, maxLength) + "...";
        }
    }
}