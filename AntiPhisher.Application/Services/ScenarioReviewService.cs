using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Response.CampaignGenerator;
using AntiPhisher.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class ScenarioReviewService : IScenarioReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScenarioReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SavedScenarioResponse> ApproveAsync(int scenarioId, int reviewedByUserId)
        {
            var scenario = await _unitOfWork.Scenarios.GetAsync(s => s.ScenarioId == scenarioId);
            if (scenario == null)
                throw new Exception($"Không tìm thấy Scenario với ID {scenarioId}.");

            if (scenario.GenerationStatus != "PendingReview")
                throw new Exception($"Scenario này đang ở trạng thái '{scenario.GenerationStatus}', không thể duyệt (chỉ duyệt được Scenario đang PendingReview).");

            scenario.GenerationStatus = "Approved";
            scenario.IsActive = true; // CHÍNH THỨC vào pool sử dụng cho Campaign thật
            scenario.ReviewedByUserId = reviewedByUserId;
            scenario.ReviewedAt = DateTime.UtcNow;
            scenario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();

            return MapToResponse(scenario);
        }

        public async Task<SavedScenarioResponse> RejectAsync(int scenarioId, string reason, int reviewedByUserId)
        {
            var scenario = await _unitOfWork.Scenarios.GetAsync(s => s.ScenarioId == scenarioId);
            if (scenario == null)
                throw new Exception($"Không tìm thấy Scenario với ID {scenarioId}.");

            if (scenario.GenerationStatus != "PendingReview")
                throw new Exception($"Scenario này đang ở trạng thái '{scenario.GenerationStatus}', không thể từ chối (chỉ từ chối được Scenario đang PendingReview).");

            scenario.GenerationStatus = "Rejected";
            scenario.IsActive = false; // giữ false — KHÔNG đưa vào pool, KHÔNG xóa cứng (phục vụ audit)
            scenario.ReviewedByUserId = reviewedByUserId;
            scenario.ReviewedAt = DateTime.UtcNow;
            scenario.UpdatedAt = DateTime.UtcNow;
            // Lưu lý do vào Description — tái dùng cột có sẵn, không tạo cột mới chỉ cho 1 mục đích nhỏ này
            scenario.Description = $"[REJECTED: {reason}] {scenario.Description}";

            await _unitOfWork.SaveChangeAsync();

            return MapToResponse(scenario);
        }

        public async Task<List<SavedScenarioResponse>> GetPendingReviewAsync()
        {
            var pending = await _unitOfWork.Scenarios.GetAllAsync(
                s => s.GenerationStatus == "PendingReview");

            return (pending ?? new List<Scenario>())
                .OrderByDescending(s => s.CreatedAt)
                .Select(MapToResponse)
                .ToList();
        }

        private static SavedScenarioResponse MapToResponse(Scenario scenario)
        {
            return new SavedScenarioResponse
            {
                ScenarioId = scenario.ScenarioId,
                Title = scenario.Title,
                GenerationStatus = scenario.GenerationStatus,
                CategoryId = scenario.CategoryId,
                DifficultyId = scenario.DifficultyId
            };
        }
    }
}