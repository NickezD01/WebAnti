using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface IOpenRouterAnalysisService
    {
        Task<string> AnalyzeCampaignActionAsync(string emailContent, string userAction);

        Task<string> AnalyzeScenarioAttemptAsync(
            string emailSubject,
            string senderEmail,
            string emailBodyHtml,
            string? phishingIndicatorsHint,
            bool isPhishingScenario,
            bool isClickedLink,
            bool isCredentialLeaked,
            bool isReported,
            bool isCorrect);
    }
}