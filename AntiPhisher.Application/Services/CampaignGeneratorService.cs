using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.CampaignGenerator;
using AntiPhisher.Application.Response.CampaignGenerator;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class CampaignGeneratorService : ICampaignGeneratorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOpenRouterAnalysisService _aiService;

        public CampaignGeneratorService(IUnitOfWork unitOfWork, IOpenRouterAnalysisService aiService)
        {
            _unitOfWork = unitOfWork;
            _aiService = aiService;
        }

        public async Task<ScenarioPreviewResponse> GeneratePreviewAsync(int knowledgeId, int difficultyId)
        {
            // 1. Lấy context Admin đã nhập (đã làm ở Bước 4.1)
            var knowledge = await _unitOfWork.AIExpandedKnowledges.GetAsync(
                k => k.Id == knowledgeId && k.IsActive);

            if (knowledge == null)
                throw new Exception($"Không tìm thấy hoặc context với ID {knowledgeId} đã bị tắt.");

            // 2. Lấy tên độ khó thật (để đưa vào prompt cho tự nhiên hơn số ID)
            var difficulty = await _unitOfWork.DifficultyLevels.GetAsync(d => d.DifficultyId == difficultyId);
            if (difficulty == null)
                throw new Exception($"Không tìm thấy mức độ khó với ID {difficultyId}.");

            // 3. Build few-shot JSON từ ĐÚNG 1 context này (đơn giản hóa — chưa cần lấy nhiều context cùng lúc)
            var fewShotPayload = new
            {
                title = knowledge.Title,
                context = knowledge.ContextDescription,
                sample = knowledge.SampleEmailContent
            };
            var fewShotJson = JsonSerializer.Serialize(fewShotPayload);

            // 4. Gọi AI sinh nội dung
            var aiRawJson = await _aiService.GenerateScenarioFromContextAsync(difficulty.LevelName, fewShotJson);

            // 5. Parse JSON — KHÔNG lưu DB ở bước này
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

            preview.DifficultyId = difficultyId;
            preview.SourceKnowledgeId = knowledgeId;

            return preview;
        }

        // =========================================================
        // BƯỚC 4.4 — Lưu preview đã được Admin xác nhận thành Scenario thật
        // =========================================================
        public async Task<SavedScenarioResponse> SaveAsScenarioAsync(SaveGeneratedScenarioRequest request, int createdByUserId)
        {
            // 1. Validate Category + Difficulty tồn tại thật (tránh FK lỗi khó hiểu lúc SaveChanges)
            var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryId == request.CategoryId);
            if (category == null)
                throw new Exception($"Không tìm thấy Category với ID {request.CategoryId}.");

            var difficulty = await _unitOfWork.DifficultyLevels.GetAsync(d => d.DifficultyId == request.DifficultyId);
            if (difficulty == null)
                throw new Exception($"Không tìm thấy mức độ khó với ID {request.DifficultyId}.");

            // 2. Chống trùng lặp — hash Title + EmailBodyHtml, check đã tồn tại Scenario giống chưa
            var contentHash = ComputeContentHash(request.Title, request.EmailBodyHtml);

            var isDuplicate = await _unitOfWork.Scenarios.GetAsync(
                s => s.ContentHash != null && s.ContentHash == contentHash);

            if (isDuplicate != null)
                throw new Exception("Nội dung này đã tồn tại trong hệ thống (trùng với 1 Scenario khác), vui lòng tạo lại preview.");

            // 3. Tạo Scenario thật — Status = PendingReview, KHÔNG tự động Active
            var scenario = new Scenario
            {
                CategoryId = request.CategoryId,
                DifficultyId = request.DifficultyId,
                CreatedByUserId = createdByUserId,
                Title = request.Title,
                Description = request.ExplanationHint,
                IsPhishing = request.IsPhishing,
                IsAIGenerated = true,
                IsActive = false, // CHƯA active — đúng nguyên tắc "Admin duyệt trước khi vào pool thật"
                SenderName = request.SenderName,
                SenderEmail = request.SenderEmail,
                RecipientName = request.RecipientName,
                Subject = request.Subject,
                EmailBodyHtml = request.EmailBodyHtml,
                PhishingIndicators = request.PhishingIndicators,
                ExplanationHint = request.ExplanationHint,
                GenerationStatus = "PendingReview",
                SourceScenarioIds = request.SourceKnowledgeId.ToString(), // tái dùng cột này để lưu nguồn gốc (KnowledgeId)
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
    }
}