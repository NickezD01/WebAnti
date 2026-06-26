using AntiPhisher.Application.Request.TemplateExpansion;
using AntiPhisher.Application.Response.CampaignGenerator;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ITemplateExpansionService
    {
        /// <summary>
        /// Sinh PREVIEW 1 Scenario biến thể từ các Scenario đã có (cùng Difficulty/Category).
        /// Chưa lưu DB.
        /// </summary>
        Task<ScenarioPreviewResponse> GenerateSimilarPreviewAsync(TemplateExpansionRequest request);
        Task<SavedScenarioResponse> SaveAsScenarioAsync(SaveSimilarScenarioRequest request, int createdByUserId);
    }
}