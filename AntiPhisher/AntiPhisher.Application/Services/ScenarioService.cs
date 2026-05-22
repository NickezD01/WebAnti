using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.AttemptRequest;
using AntiPhisher.Application.Request.ScenarioRequest;
using AntiPhisher.Application.Response.AttemptRespond;
using AntiPhisher.Application.Response.ScenarioRespond;
using AntiPhisher.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class ScenarioService : IScenarioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly HttpClient _httpClient;
        private readonly IMapper _mapper;
        private readonly string _apiKey;

        public ScenarioService(IUnitOfWork unitOfWork, HttpClient httpClient, IConfiguration configuration, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _httpClient = httpClient;
            _mapper = mapper;
            _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
        }

        // =========================================================
        // LOGIC LÀM BÀI TEST & PHÂN TÍCH AI (GIỮ NGUYÊN GỐC)
        // =========================================================
        public async Task<AttemptResultResponse> SubmitAttemptAsync(SubmitAttemptRequest request, int userId)
        {
            // 1. Kiểm tra sự tồn tại của kịch bản
            var scenario = await _unitOfWork.Scenarios.GetAsync(
                filter: s => s.ScenarioId == request.ScenarioId,
                include: query => query.Include(s => s.Difficulty)
            );

            if (scenario == null)
                throw new Exception($"Không tìm thấy kịch bản với ID {request.ScenarioId}.");

            // 2. Xử lý logic tính điểm
            bool userThinksPhishing = request.UserAnswer.Equals("Phishing", StringComparison.OrdinalIgnoreCase);
            bool isCorrect = (userThinksPhishing == scenario.IsPhishing);
            int scoreEarned = isCorrect ? (scenario.Difficulty?.BaseScore ?? 0) : 0;

            // 3. Đếm số lần thử của người dùng
            var pastAttempts = await _unitOfWork.UserAttempts.GetAllAsync(
                filter: a => a.UserId == userId && a.ScenarioId == request.ScenarioId
            );
            int currentAttemptCount = pastAttempts?.Count ?? 0;

            // 4. Khởi tạo đối tượng Attempt
            var attempt = new UserAttempt
            {
                UserId = userId,
                ScenarioId = request.ScenarioId,
                CampaignId = request.CampaignId,
                // Lưu ý: Đảm bảo đã chạy lệnh ALTER TABLE ALTER COLUMN UserAnswer NVARCHAR(500)
                UserAnswer = request.UserAnswer,
                IsCorrect = isCorrect,
                Score = scoreEarned,
                TimeTakenSeconds = request.TimeTakenSeconds,
                AttemptNumber = currentAttemptCount + 1,
                SubmittedAt = DateTime.UtcNow
            };

            try
            {
                // 5. Lưu kết quả bài làm vào DB
                await _unitOfWork.UserAttempts.AddAsync(attempt);
                await _unitOfWork.SaveChangeAsync();

                // 6. Gọi AI phân tích
                var aiFeedback = await FetchAIFeedbackFromOpenAI(scenario, isCorrect, request.UserAnswer);
                aiFeedback.AttemptId = attempt.AttemptId;

                // 7. Lưu phản hồi của AI
                await _unitOfWork.AIFeedbacks.AddAsync(aiFeedback);
                await _unitOfWork.SaveChangeAsync();

                return new AttemptResultResponse
                {
                    AttemptId = attempt.AttemptId,
                    IsCorrect = isCorrect,
                    ScoreEarned = scoreEarned,
                    FeedbackText = aiFeedback.FeedbackText ?? "Không có phản hồi.",
                    IndicatorsExplained = aiFeedback.IndicatorsExplained ?? "Không có giải thích.",
                    ImprovementTips = aiFeedback.ImprovementTips ?? "Không có mẹo.",
                    AIModel = aiFeedback.AIModel ?? "gpt-4o"
                };
            }
            catch (DbUpdateException ex)
            {
                // Trích xuất chi tiết lỗi từ SQL Server (thường do độ dài chuỗi hoặc FK constraint)
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new Exception($"Lỗi lưu vào database: {message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra trong quá trình xử lý: {ex.Message}");
            }
        }

        // =========================================================
        // LOGIC CÁC HÀM CRUD MỚI (ĐÃ CHUẨN HÓA TOÀN DIỆN)
        // =========================================================

        // 1. LẤY TẤT CẢ KỊCH BẢN (Nạp kèm thông tin bảng DifficultyLevel)
        public async Task<IEnumerable<ScenarioDetailResponse>> GetAllScenariosAsync()
        {
            var scenarios = await _unitOfWork.Scenarios.GetAllAsync(
                filter: null,
                include: query => query.Include(s => s.Difficulty),
                pageIndex: 1,
                pageSize: 100
            );
            return _mapper.Map<IEnumerable<ScenarioDetailResponse>>(scenarios);
        }

        // 2. LẤY CHI TIẾT KỊCH BẢN THEO ID
        public async Task<ScenarioDetailResponse?> GetScenarioByIdAsync(int id)
        {
            var scenario = await _unitOfWork.Scenarios.GetAsync(
                filter: s => s.ScenarioId == id,
                include: query => query.Include(s => s.Difficulty)
            );
            return scenario == null ? null : _mapper.Map<ScenarioDetailResponse>(scenario);
        }

        // 3. TẠO MỚI KỊCH BẢN (Xử lý thông qua Mapper cấu hình Fallback thông minh)
        public async Task<ScenarioDetailResponse> CreateScenarioAsync(CreateScenarioRequest request)
        {
            var scenario = _mapper.Map<Scenario>(request);

            scenario.CreatedAt = DateTime.UtcNow;
            scenario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Scenarios.AddAsync(scenario);
            await _unitOfWork.SaveChangeAsync();

            var result = await _unitOfWork.Scenarios.GetAsync(
                filter: s => s.ScenarioId == scenario.ScenarioId,
                include: query => query.Include(s => s.Difficulty)
            );

            return _mapper.Map<ScenarioDetailResponse>(result);
        }

        // 4. CẬP NHẬT KỊCH BẢN 
        public async Task<ScenarioDetailResponse> UpdateScenarioAsync(int id, UpdateScenarioRequest request)
        {
            var existingScenario = await _unitOfWork.Scenarios.GetAsync(filter: s => s.ScenarioId == id);
            if (existingScenario == null)
                throw new Exception($"Không tìm thấy kịch bản ID {id} để cập nhật.");

            _mapper.Map(request, existingScenario);
            existingScenario.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();

            var result = await _unitOfWork.Scenarios.GetAsync(
                filter: s => s.ScenarioId == id,
                include: query => query.Include(s => s.Difficulty)
            );
            return _mapper.Map<ScenarioDetailResponse>(result);
        }

        // 5. XÓA KỊCH BẢN (ĐÃ FIX LỖI VOID)
        public async Task<bool> DeleteScenarioAsync(int id)
        {
            var existingScenario = await _unitOfWork.Scenarios.GetAsync(filter: s => s.ScenarioId == id);
            if (existingScenario == null)
                throw new Exception($"Không tìm thấy kịch bản ID {id} để xóa.");

            await _unitOfWork.Scenarios.RemoveByIdAsync(id);
            await _unitOfWork.SaveChangeAsync(); // Đã xóa việc gán vào biến 'affectedRows' ở đây

            // Kiểm tra lại xem bản ghi đã thực sự biến mất khỏi DB chưa để trả về true/false an toàn
            var checkDeleted = await _unitOfWork.Scenarios.GetAsync(filter: s => s.ScenarioId == id);
            return checkDeleted == null;
        }

        // =========================================================
        // HÀM PRIVATE GỌI OPENAI GPT-4O (GIỮ NGUYÊN GỐC)
        // =========================================================
        private async Task<AIFeedback> FetchAIFeedbackFromOpenAI(Scenario scenario, bool isCorrect, string userAnswer)
        {
            var fallbackFeedback = new AIFeedback
            {
                FeedbackText = "Hệ thống phân tích AI đang bận, vui lòng kiểm tra lại phản hồi chi tiết trong lịch sử.",
                IndicatorsExplained = "Không thể phân tích dấu hiệu.",
                ImprovementTips = "Hãy cẩn trọng với các email yêu cầu hành động khẩn cấp.",
                AIModel = "gpt-4o",
                CreatedAt = DateTime.UtcNow
            };

            if (string.IsNullOrEmpty(_apiKey)) return fallbackFeedback;

            string systemPrompt = "Bạn là chuyên gia phân tích bảo mật thông tin (Phishing Security Expert). " +
                                  "Hãy đưa ra phản hồi bằng tiếng Việt dưới định dạng JSON Object chứa chính xác 3 key: " +
                                  "\"feedbackText\" (nhận xét lựa chọn của user), " +
                                  "\"indicatorsExplained\" (giải thích các điểm nghi vấn/an toàn trong nội dung email), " +
                                  "\"improvementTips\" (mẹo bảo mật cốt lõi).";

            string userPrompt = $"[Thông tin Email]\n" +
                               $"- Tiêu đề: {scenario.Subject}\n" +
                               $"- Người gửi: {scenario.SenderEmail}\n" +
                               $"- Nội dung HTML: {scenario.EmailBodyHtml}\n" +
                               $"- Dấu hiệu lừa đảo gốc: {scenario.PhishingIndicators}\n\n" +
                               $"[Kết quả bài làm]\n" +
                               $"- Người dùng đoán là: {userAnswer}\n" +
                               $"- Kết quả: {(isCorrect ? "ĐÚNG" : "SAI")}";

            var requestBody = new
            {
                model = "gpt-4o",
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            try
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                requestMessage.Headers.Add("Authorization", $"Bearer {_apiKey}");
                requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(requestMessage);
                if (!response.IsSuccessStatusCode) return fallbackFeedback;

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                var contentJson = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                var tokensUsed = doc.RootElement.GetProperty("usage").GetProperty("total_tokens").GetInt32();

                using var contentDoc = JsonDocument.Parse(contentJson ?? "{}");

                return new AIFeedback
                {
                    FeedbackText = contentDoc.RootElement.GetProperty("feedbackText").GetString(),
                    IndicatorsExplained = contentDoc.RootElement.GetProperty("indicatorsExplained").GetString(),
                    ImprovementTips = contentDoc.RootElement.GetProperty("improvementTips").GetString(),
                    AIModel = "gpt-4o",
                    PromptTokensUsed = tokensUsed,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return fallbackFeedback;
            }
        }
    }
}