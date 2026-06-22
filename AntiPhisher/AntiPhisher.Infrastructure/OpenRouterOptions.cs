using System;
using System.Net.Http;
using AntiPhisher.Application.Interfaces;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace AntiPhisher.Infrastructure
{
    // Class config riêng — KHÔNG chứa logic gọi API, chỉ chứa setting
    public class OpenRouterOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string Model { get; set; } = "openrouter/free";
        public string FallbackModel { get; set; } = "deepseek/deepseek-chat-v3:free";
    }

    // Class này mới thực sự gọi API — đổi tên để không nhầm với Options
    public class OpenRouterAnalysisService : IOpenRouterAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenRouterOptions _options;
        private readonly ILogger<OpenRouterAnalysisService> _logger;

        public OpenRouterAnalysisService(
            HttpClient httpClient,
            IOptions<OpenRouterOptions> options,
            ILogger<OpenRouterAnalysisService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://antiphisher.local");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "AntiPhisher");
        }

        public Task<string> AnalyzeCampaignActionAsync(string emailContent, string userAction)
            => AnalyzeCampaignActionAsync(emailContent, userAction, useFallback: false);

        private async Task<string> AnalyzeCampaignActionAsync(string emailContent, string userAction, bool useFallback)
        {
            var model = useFallback ? _options.FallbackModel : _options.Model;
            var url = $"{_options.BaseUrl}/chat/completions";

            string systemPrompt =
                "Bạn là chuyên gia đánh giá An toàn thông tin của hệ thống AntiPhisher. " +
                "Tôi sẽ cung cấp nội dung của một email lừa đảo (Campaign) và hành động thực tế của người dùng. " +
                "Hãy phân tích xem hành động đó Đúng hay Sai, chỉ ra các dấu hiệu sơ hở, lý do tại sao, và đưa ra lời khuyên ngắn gọn. " +
                "BẮT BUỘC trả về CHỈ JSON thuần túy, không kèm ký tự hay lời dẫn nào khác, theo cấu trúc: " +
                "{\"isCorrect\": true/false, \"detectedFlaw\": \"...\", \"reason\": \"...\", \"advice\": \"...\"}";

            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"[Nội dung email lừa đảo]: {emailContent}\n[Hành động của người dùng]: {userAction}" }
                },
                response_format = new { type = "json_object" },
                temperature = 0.3
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouter call failed: model={Model}, status={Status}", model, response.StatusCode);

                // Tự động chuyển sang fallback model nếu bị rate limit / hết credit
                if (!useFallback && (response.StatusCode == HttpStatusCode.TooManyRequests
                                      || response.StatusCode == HttpStatusCode.PaymentRequired))
                {
                    return await AnalyzeCampaignActionAsync(emailContent, userAction, useFallback: true);
                }

                return "{\"isCorrect\": false, \"detectedFlaw\": \"Lỗi kết nối AI\", " +
                       "\"reason\": \"Không thể kết nối đến OpenRouter API.\", " +
                       "\"advice\": \"Hãy kiểm tra lại API Key hoặc mạng internet.\"}";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("=== OPENROUTER RAW RESPONSE === {Response}", jsonResponse);

            using var doc = JsonDocument.Parse(jsonResponse);
            var aiTextResult = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _logger.LogInformation("=== AI EXTRACTED TEXT === {Text}", aiTextResult);

            return aiTextResult ?? string.Empty;
        }
        public Task<string> AnalyzeScenarioAttemptAsync(
    string emailSubject,
    string senderEmail,
    string emailBodyHtml,
    string? phishingIndicatorsHint,
    bool isPhishingScenario,
    bool isClickedLink,
    bool isCredentialLeaked,
    bool isReported,
    bool isCorrect)
    => AnalyzeScenarioAttemptAsync(
        emailSubject, senderEmail, emailBodyHtml, phishingIndicatorsHint,
        isPhishingScenario, isClickedLink, isCredentialLeaked, isReported, isCorrect,
        useFallback: false);

        private async Task<string> AnalyzeScenarioAttemptAsync(
            string emailSubject,
            string senderEmail,
            string emailBodyHtml,
            string? phishingIndicatorsHint,
            bool isPhishingScenario,
            bool isClickedLink,
            bool isCredentialLeaked,
            bool isReported,
            bool isCorrect,
            bool useFallback)
        {
            var model = useFallback ? _options.FallbackModel : _options.Model;
            var url = $"{_options.BaseUrl}/chat/completions";

            string systemPrompt =
                "Bạn là chuyên gia đánh giá An toàn thông tin của hệ thống AntiPhisher, đang chấm bài " +
                "cho một bài tập mô phỏng nhận diện email phishing (Scenario). " +
                "Dựa vào nội dung email và hành vi thực tế của học viên, hãy viết phản hồi giáo dục, " +
                "khuyến khích, dễ hiểu. " +
                "BẮT BUỘC trả về CHỈ JSON thuần túy, không kèm ký tự hay lời dẫn nào khác, theo cấu trúc: " +
                "{\"feedbackText\": \"...\", \"indicatorsExplained\": \"...\", \"improvementTips\": \"...\"}";

            string behaviorDesc =
                $"- Đã bấm vào link giả mạo: {(isClickedLink ? "CÓ" : "KHÔNG")}\n" +
                $"- Đã nhập thông tin đăng nhập/thẻ: {(isCredentialLeaked ? "CÓ" : "KHÔNG")}\n" +
                $"- Đã báo cáo email đáng ngờ: {(isReported ? "CÓ" : "KHÔNG")}";

            string userPrompt =
                $"[Thông tin Email]\n" +
                $"- Đây có phải email phishing thật không: {(isPhishingScenario ? "CÓ" : "KHÔNG, đây là email an toàn")}\n" +
                $"- Tiêu đề: {emailSubject}\n" +
                $"- Người gửi: {senderEmail}\n" +
                $"- Nội dung HTML: {emailBodyHtml}\n" +
                $"- Gợi ý dấu hiệu lừa đảo (do Admin định nghĩa, có thể trống): {phishingIndicatorsHint}\n\n" +
                $"[Hành vi của học viên]\n{behaviorDesc}\n" +
                $"- Kết quả đánh giá theo rule hệ thống: {(isCorrect ? "ĐÚNG" : "SAI")}";

            var requestBody = new
            {
                model,
                messages = new object[]
                {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.3
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouter (ScenarioAttempt) call failed: model={Model}, status={Status}", model, response.StatusCode);

                if (!useFallback && (response.StatusCode == HttpStatusCode.TooManyRequests
                                      || response.StatusCode == HttpStatusCode.PaymentRequired))
                {
                    return await AnalyzeScenarioAttemptAsync(
                        emailSubject, senderEmail, emailBodyHtml, phishingIndicatorsHint,
                        isPhishingScenario, isClickedLink, isCredentialLeaked, isReported, isCorrect,
                        useFallback: true);
                }

                return "{\"feedbackText\": \"Hệ thống AI đang bận, không thể phân tích chi tiết lúc này.\", " +
                       "\"indicatorsExplained\": \"Không thể phân tích dấu hiệu do lỗi kết nối AI.\", " +
                       "\"improvementTips\": \"Hãy luôn kiểm tra kỹ địa chỉ người gửi và đường link trước khi click.\"}";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("=== OPENROUTER (ScenarioAttempt) RAW RESPONSE === {Response}", jsonResponse);

            using var doc = JsonDocument.Parse(jsonResponse);
            var aiTextResult = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiTextResult ?? string.Empty;
        }
    }
}