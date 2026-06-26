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
    public class OpenRouterOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
        public string Model { get; set; } = "openrouter/free";
        public string FallbackModel { get; set; } = "deepseek/deepseek-chat-v3:free";
    }

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

        public Task<string> GenerateUserPredictiveAdviceAsync(
            string tacticStatsJson,
            int totalAttempts,
            int totalFlaws)
            => GenerateUserPredictiveAdviceAsync(tacticStatsJson, totalAttempts, totalFlaws, useFallback: false);

        private async Task<string> GenerateUserPredictiveAdviceAsync(
            string tacticStatsJson,
            int totalAttempts,
            int totalFlaws,
            bool useFallback)
        {
            var model = useFallback ? _options.FallbackModel : _options.Model;
            var url = $"{_options.BaseUrl}/chat/completions";

            string systemPrompt =
                "Bạn là chuyên gia phân tích hành vi an toàn thông tin (security awareness analyst) " +
                "của hệ thống AntiPhisher. Dựa trên dữ liệu lịch sử lỗi của một học viên trong " +
                "các bài mô phỏng phishing, hãy phân tích pattern lỗi và đưa ra lời khuyên hành động " +
                "ngắn gọn, cụ thể, mang tính khích lệ — không chỉ trích. " +
                "BẮT BUỘC trả về CHỈ JSON thuần túy, không kèm ký tự hay lời dẫn nào khác, theo cấu trúc: " +
                "{\"riskPattern\": \"...\", \"primaryWeakness\": \"...\", \"confidenceScore\": 0.0, " +
                "\"actionableAdvice\": [\"...\", \"...\"], \"recommendedNextDifficulty\": \"Easy|Medium|Hard\"}";

            string userPrompt =
                $"Dữ liệu lỗi theo danh mục (90 ngày gần nhất, top 5): {tacticStatsJson}\n" +
                $"Tổng số lần làm bài: {totalAttempts}. Tổng số lần mắc lỗi: {totalFlaws}.";

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
                _logger.LogWarning("OpenRouter (PredictiveAdvice) call failed: model={Model}, status={Status}", model, response.StatusCode);

                if (!useFallback && (response.StatusCode == HttpStatusCode.TooManyRequests
                                      || response.StatusCode == HttpStatusCode.PaymentRequired))
                {
                    return await GenerateUserPredictiveAdviceAsync(tacticStatsJson, totalAttempts, totalFlaws, useFallback: true);
                }

                return "{\"riskPattern\": \"Không thể phân tích lúc này\", " +
                       "\"primaryWeakness\": \"Không xác định\", \"confidenceScore\": 0, " +
                       "\"actionableAdvice\": [\"Hệ thống AI đang bận, vui lòng thử lại sau.\"], " +
                       "\"recommendedNextDifficulty\": \"Medium\"}";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("=== OPENROUTER (PredictiveAdvice) RAW RESPONSE === {Response}", jsonResponse);

            using var doc = JsonDocument.Parse(jsonResponse);
            var aiTextResult = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

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

        public Task<string> GenerateExecutiveSummaryAsync(
            string companyName,
            int totalEmployees,
            decimal orgFailRate,
            string tacticBreakdownJson)
            => GenerateExecutiveSummaryAsync(companyName, totalEmployees, orgFailRate, tacticBreakdownJson, useFallback: false);

        private async Task<string> GenerateExecutiveSummaryAsync(
            string companyName,
            int totalEmployees,
            decimal orgFailRate,
            string tacticBreakdownJson,
            bool useFallback)
        {
            var model = useFallback ? _options.FallbackModel : _options.Model;
            var url = $"{_options.BaseUrl}/chat/completions";

            string systemPrompt =
                "Bạn là chuyên gia phân tích an toàn thông tin viết báo cáo cấp quản lý (executive summary) " +
                "về kết quả huấn luyện chống phishing của một tổ chức trong hệ thống AntiPhisher. " +
                "Văn phong ngắn gọn, định hướng hành động, phù hợp gửi cho ban lãnh đạo. " +
                "BẮT BUỘC trả về CHỈ JSON thuần túy, không kèm ký tự hay lời dẫn nào khác, theo cấu trúc: " +
                "{\"executiveSummary\": \"...\", \"topRisks\": [\"...\", \"...\"], " +
                "\"suggestedNextCampaignContext\": \"...\", \"suggestedDifficulty\": \"Easy|Medium|Hard\"}";

            string userPrompt =
                $"Công ty: {companyName}\n" +
                $"Tổng số nhân viên: {totalEmployees}\n" +
                $"Tỉ lệ mắc lỗi toàn công ty (90 ngày gần nhất): {orgFailRate}%\n" +
                $"Phân tích theo loại kỹ thuật lừa đảo (category): {tacticBreakdownJson}";

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
                _logger.LogWarning("OpenRouter (ExecutiveSummary) call failed: model={Model}, status={Status}", model, response.StatusCode);

                if (!useFallback && (response.StatusCode == HttpStatusCode.TooManyRequests
                                      || response.StatusCode == HttpStatusCode.PaymentRequired))
                {
                    return await GenerateExecutiveSummaryAsync(companyName, totalEmployees, orgFailRate, tacticBreakdownJson, useFallback: true);
                }

                return "{\"executiveSummary\": \"Không thể phân tích lúc này, vui lòng thử lại sau.\", " +
                       "\"topRisks\": [], \"suggestedNextCampaignContext\": \"\", \"suggestedDifficulty\": \"Medium\"}";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("=== OPENROUTER (ExecutiveSummary) RAW RESPONSE === {Response}", jsonResponse);

            using var doc = JsonDocument.Parse(jsonResponse);
            var aiTextResult = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiTextResult ?? string.Empty;
        }

        public Task<string> GenerateScenarioFromContextAsync(
            string difficultyName,
            string fewShotContextJson)
            => GenerateScenarioFromContextAsync(difficultyName, fewShotContextJson, useFallback: false);

        private async Task<string> GenerateScenarioFromContextAsync(
            string difficultyName,
            string fewShotContextJson,
            bool useFallback)
        {
            var model = useFallback ? _options.FallbackModel : _options.Model;
            var url = $"{_options.BaseUrl}/chat/completions";

            string systemPrompt =
                "Bạn là chuyên gia thiết kế tình huống đào tạo nhận diện email lừa đảo (phishing " +
                "awareness training) cho mục đích giáo dục nội bộ trong tổ chức thuộc hệ thống AntiPhisher. " +
                "Nhiệm vụ của bạn là soạn một email giả lập (simulation email) dùng để TEST khả năng " +
                "nhận diện của nhân viên — không phải để tấn công thật. " +
                "Email phải có các dấu hiệu lừa đảo điển hình nhưng PHẢI rõ ràng là nội dung mô phỏng " +
                "nội bộ, dùng domain giả lập dạng *.phishtraining.internal. " +
                "TRẢ LỜI NGẮN GỌN, súc tích, không suy luận dài dòng, không giải thích thêm. " +
                "BẮT BUỘC trả về CHỈ JSON thuần túy, không kèm ký tự hay lời dẫn nào khác, theo cấu trúc: " +
                "{\"title\": \"...\", \"senderName\": \"...\", \"senderEmail\": \"...\", " +
                "\"recipientName\": \"...\", \"subject\": \"...\", \"emailBodyHtml\": \"...\", " +
                "\"phishingIndicators\": \"...\", \"explanationHint\": \"...\", \"isPhishing\": true}";

            string userPrompt =
                $"Mức độ khó yêu cầu: {difficultyName}\n" +
                $"Ngữ cảnh tham khảo (do Admin cung cấp, dùng làm cảm hứng — KHÔNG sao chép nguyên văn):\n" +
                $"{fewShotContextJson}\n\n" +
                $"Hãy tạo MỘT email giả lập mới, lấy cảm hứng từ ngữ cảnh trên nhưng có tình huống cụ thể " +
                $"khác biệt, phù hợp với bối cảnh doanh nghiệp công nghệ tại Việt Nam.";

            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.7,
                max_tokens = 1500
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouter (GenerateScenario) call failed: model={Model}, status={Status}", model, response.StatusCode);

                if (!useFallback && (response.StatusCode == HttpStatusCode.TooManyRequests
                                      || response.StatusCode == HttpStatusCode.PaymentRequired))
                {
                    return await GenerateScenarioFromContextAsync(difficultyName, fewShotContextJson, useFallback: true);
                }

                return BuildScenarioErrorFallbackJson();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("=== OPENROUTER (GenerateScenario) RAW RESPONSE === {Response}", jsonResponse);

            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("choices", out var choicesElement) || choicesElement.GetArrayLength() == 0)
            {
                var errorMsg = doc.RootElement.TryGetProperty("error", out var errEl)
                    ? errEl.ToString()
                    : "Không xác định";
                _logger.LogWarning("OpenRouter (GenerateScenario) returned 200 but no choices. Error: {Error}", errorMsg);

                if (!useFallback)
                {
                    return await GenerateScenarioFromContextAsync(difficultyName, fewShotContextJson, useFallback: true);
                }

                return BuildScenarioErrorFallbackJson();
            }

            var aiTextResult = choicesElement[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiTextResult ?? BuildScenarioErrorFallbackJson();
        }

        private static string BuildScenarioErrorFallbackJson()
        {
            return "{\"title\": \"Lỗi sinh kịch bản\", \"senderName\": \"\", \"senderEmail\": \"\", " +
                   "\"recipientName\": \"\", \"subject\": \"\", \"emailBodyHtml\": \"\", " +
                   "\"phishingIndicators\": \"\", \"explanationHint\": \"Hệ thống AI đang bận hoặc quá tải, vui lòng thử lại sau.\", " +
                   "\"isPhishing\": true}";
        }

        // =========================================================
        // BƯỚC 5.1 — Sinh Scenario biến thể từ các Scenario có sẵn (Self-Expansion)
        // =========================================================
        public Task<string> GenerateSimilarScenarioAsync(
            string difficultyName,
            string fewShotScenariosJson)
            => GenerateSimilarScenarioAsync(difficultyName, fewShotScenariosJson, useFallback: false);

        private async Task<string> GenerateSimilarScenarioAsync(
            string difficultyName,
            string fewShotScenariosJson,
            bool useFallback)
        {
            var model = useFallback ? _options.FallbackModel : _options.Model;
            var url = $"{_options.BaseUrl}/chat/completions";

            string systemPrompt =
                "Bạn là chuyên gia thiết kế email mô phỏng phishing cho mục đích đào tạo nội bộ " +
                "trong hệ thống AntiPhisher. Dưới đây là các email mẫu ĐÃ CÓ trong hệ thống ở cùng " +
                "mức độ khó. Hãy tạo MỘT email MỚI: " +
                "(1) Cùng mức độ khó, cùng 'họ' kỹ thuật tấn công (tactic) với các mẫu trên; " +
                "(2) KHÁC BIỆT rõ rệt về: chủ đề cụ thể, tên thương hiệu giả mạo, cách diễn đạt, tình huống; " +
                "(3) KHÔNG sao chép gần giống bất kỳ mẫu nào ở trên; " +
                "(4) Dùng domain giả lập dạng *.phishtraining.internal. " +
                "TRẢ LỜI NGẮN GỌN, súc tích, không suy luận dài dòng, không giải thích thêm. " +
                "BẮT BUỘC trả về CHỈ JSON thuần túy, không kèm ký tự hay lời dẫn nào khác, theo cấu trúc: " +
                "{\"title\": \"...\", \"senderName\": \"...\", \"senderEmail\": \"...\", " +
                "\"recipientName\": \"...\", \"subject\": \"...\", \"emailBodyHtml\": \"...\", " +
                "\"phishingIndicators\": \"...\", \"explanationHint\": \"...\", \"isPhishing\": true}";

            string userPrompt =
                $"Mức độ khó: {difficultyName}\n" +
                $"Các mẫu hiện có (KHÔNG được tạo email giống các mẫu này):\n" +
                $"{fewShotScenariosJson}";

            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.8,
                max_tokens = 1500
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenRouter (SimilarScenario) call failed: model={Model}, status={Status}", model, response.StatusCode);

                if (!useFallback && (response.StatusCode == HttpStatusCode.TooManyRequests
                                      || response.StatusCode == HttpStatusCode.PaymentRequired))
                {
                    return await GenerateSimilarScenarioAsync(difficultyName, fewShotScenariosJson, useFallback: true);
                }

                return BuildSimilarScenarioErrorFallbackJson();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("=== OPENROUTER (SimilarScenario) RAW RESPONSE === {Response}", jsonResponse);

            using var doc = JsonDocument.Parse(jsonResponse);

            if (!doc.RootElement.TryGetProperty("choices", out var choicesElement) || choicesElement.GetArrayLength() == 0)
            {
                var errorMsg = doc.RootElement.TryGetProperty("error", out var errEl)
                    ? errEl.ToString()
                    : "Không xác định";
                _logger.LogWarning("OpenRouter (SimilarScenario) returned 200 but no choices. Error: {Error}", errorMsg);

                if (!useFallback)
                {
                    return await GenerateSimilarScenarioAsync(difficultyName, fewShotScenariosJson, useFallback: true);
                }

                return BuildSimilarScenarioErrorFallbackJson();
            }

            var aiTextResult = choicesElement[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return aiTextResult ?? BuildSimilarScenarioErrorFallbackJson();
        }

        private static string BuildSimilarScenarioErrorFallbackJson()
        {
            return "{\"title\": \"Lỗi sinh biến thể\", \"senderName\": \"\", \"senderEmail\": \"\", " +
                   "\"recipientName\": \"\", \"subject\": \"\", \"emailBodyHtml\": \"\", " +
                   "\"phishingIndicators\": \"\", \"explanationHint\": \"Hệ thống AI đang bận hoặc quá tải, vui lòng thử lại sau.\", " +
                   "\"isPhishing\": true}";
        }
    }
}