using System.Threading.Tasks;
using AntiPhisher.Application.Request.CampaignGenerator;
using AntiPhisher.Application.Response.CampaignGenerator;

namespace AntiPhisher.Application.Interfaces
{
    public interface ICampaignGeneratorService
    {
        /// <summary>
        /// Sinh PREVIEW 1 Scenario mới từ context (AIExpandedKnowledge) — CHƯA lưu vào DB.
        /// Admin xem trước nội dung, nếu đồng ý mới gọi SaveAsScenarioAsync ở bước sau.
        /// </summary>
        Task<ScenarioPreviewResponse> GeneratePreviewAsync(int knowledgeId, int difficultyId);
        Task<SavedScenarioResponse> SaveAsScenarioAsync(SaveGeneratedScenarioRequest request, int createdByUserId);
    }
}