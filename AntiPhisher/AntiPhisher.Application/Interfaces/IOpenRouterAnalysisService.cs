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

        Task<string> GenerateUserPredictiveAdviceAsync(
            string tacticStatsJson,
            int totalAttempts,
            int totalFlaws);

        Task<string> GenerateExecutiveSummaryAsync(
            string companyName,
            int totalEmployees,
            decimal orgFailRate,
            string tacticBreakdownJson);

        Task<string> GenerateScenarioFromContextAsync(
            string difficultyName,
            string fewShotContextJson);

        Task<string> GenerateSimilarScenarioAsync(
            string difficultyName,
            string fewShotScenariosJson);
    }
}
