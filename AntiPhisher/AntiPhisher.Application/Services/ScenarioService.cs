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
using System.Text.Json;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Services
{
    public class ScenarioService : IScenarioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOpenRouterAnalysisService _openRouter;
        private readonly ILessonService _lessonService;
        private readonly IMapper _mapper;
        private readonly string _openRouterModel;

        public ScenarioService(
            IUnitOfWork unitOfWork,
            IOpenRouterAnalysisService openRouter,
            ILessonService lessonService,
            IConfiguration configuration,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _openRouter = openRouter;
            _lessonService = lessonService;
            _mapper = mapper;
            _openRouterModel = configuration["OpenRouter:Model"] ?? "openrouter/free";
        }

        // =========================================================
        // LOGIC LÀM BÀI TEST & PHÂN TÍCH AI — REFACTORED
        // Dùng 3 boolean hành vi thay vì UserAnswer string
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

            bool isPhishingScenario = scenario.IsPhishing;
            int baseScore = scenario.Difficulty?.BaseScore ?? 10;

            // 2. Tính điểm và mức rủi ro dựa trên hành vi
            var (isCorrect, scoreEarned, riskLevel) = CalculateScoreAndRisk(
                isPhishingScenario,
                request.IsClickedLink,
                request.IsCredentialLeaked,
                request.IsReported,
                baseScore);

            // 3. Chuyển đổi hành vi → UserAnswer dạng chuỗi (backward compat, lưu DB)
            string behaviorSummary = request.IsReported ? "Reported"
                : request.IsClickedLink ? "Clicked"
                : "NoAction";

            // 4. Đếm số lần thử
            var pastAttempts = await _unitOfWork.UserAttempts.GetAllAsync(
                filter: a => a.UserId == userId && a.ScenarioId == request.ScenarioId
            );
            int currentAttemptCount = pastAttempts?.Count ?? 0;

            // 5. Khởi tạo Attempt với 3 trường boolean mới
            var attempt = new UserAttempt
            {
                UserId = userId,
                ScenarioId = request.ScenarioId,
                CampaignId = request.CampaignId,
                UserAnswer = behaviorSummary,      // computed from behavior
                IsCorrect = isCorrect,
                Score = scoreEarned,
                TimeTakenSeconds = request.TimeTakenSeconds,
                AttemptNumber = currentAttemptCount + 1,
                SubmittedAt = DateTime.UtcNow,
                // NEW behavior fields
                IsClickedLink = request.IsClickedLink,
                IsCredentialLeaked = request.IsCredentialLeaked,
                IsReported = request.IsReported
            };

            try
            {
                await _unitOfWork.UserAttempts.AddAsync(attempt);
                await _unitOfWork.SaveChangeAsync();

                // 6. Gate: chỉ gọi AI nếu user đã có gói trả phí
                bool unlocked = await _lessonService.IsUserUnlockedAsync(userId);
                if (!unlocked)
                {
                    return new AttemptResultResponse
                    {
                        AttemptId          = attempt.AttemptId,
                        IsCorrect          = isCorrect,
                        ScoreEarned        = scoreEarned,
                        IsClickedLink      = request.IsClickedLink,
                        IsCredentialLeaked = request.IsCredentialLeaked,
                        IsReported         = request.IsReported,
                        RiskLevel          = riskLevel,
                        IsAiLocked         = true
                    };
                }

                // 7. Gọi OpenRouter + parse JSON phòng thủ
                var aiFeedback = new AIFeedback
                {
                    AttemptId        = attempt.AttemptId,
                    AIModel          = _openRouterModel,
                    PromptTokensUsed = null,
                    CreatedAt        = DateTime.UtcNow
                };

                try
                {
                    var rawJson = await _openRouter.AnalyzeScenarioAttemptAsync(
                        scenario.Subject,
                        scenario.SenderEmail,
                        scenario.EmailBodyHtml,
                        scenario.PhishingIndicators,
                        isPhishingScenario,
                        request.IsClickedLink,
                        request.IsCredentialLeaked,
                        request.IsReported,
                        isCorrect);

                    var cleaned = rawJson.Trim();
                    if (cleaned.StartsWith("```"))
                    {
                        var start = cleaned.IndexOf('\n') + 1;
                        var end   = cleaned.LastIndexOf("```");
                        if (end > start) cleaned = cleaned[start..end].Trim();
                    }

                    using var doc = JsonDocument.Parse(cleaned);
                    var root = doc.RootElement;
                    aiFeedback.FeedbackText        = root.GetProperty("feedbackText").GetString()        ?? string.Empty;
                    aiFeedback.IndicatorsExplained = root.GetProperty("indicatorsExplained").GetString() ?? string.Empty;
                    aiFeedback.ImprovementTips     = root.GetProperty("improvementTips").GetString()     ?? string.Empty;
                }
                catch
                {
                    aiFeedback.FeedbackText        = "Hệ thống AI đang bận, không thể phân tích chi tiết lúc này.";
                    aiFeedback.IndicatorsExplained = "Không thể phân tích dấu hiệu do lỗi kết nối AI.";
                    aiFeedback.ImprovementTips     = "Hãy luôn kiểm tra kỹ địa chỉ người gửi và đường link trước khi click.";
                    aiFeedback.AIModel             = "fallback";
                }

                await _unitOfWork.AIFeedbacks.AddAsync(aiFeedback);
                await _unitOfWork.SaveChangeAsync();

                return new AttemptResultResponse
                {
                    AttemptId           = attempt.AttemptId,
                    IsCorrect           = isCorrect,
                    ScoreEarned         = scoreEarned,
                    IsClickedLink       = request.IsClickedLink,
                    IsCredentialLeaked  = request.IsCredentialLeaked,
                    IsReported          = request.IsReported,
                    RiskLevel           = riskLevel,
                    IsAiLocked          = false,
                    FeedbackText        = aiFeedback.FeedbackText        ?? "Không có phản hồi.",
                    IndicatorsExplained = aiFeedback.IndicatorsExplained ?? "Không có giải thích.",
                    ImprovementTips     = aiFeedback.ImprovementTips     ?? "Không có mẹo.",
                    AIModel             = aiFeedback.AIModel             ?? "openrouter"
                };
            }
            catch (DbUpdateException ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Lỗi lưu vào database: {message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Có lỗi xảy ra trong quá trình xử lý: {ex.Message}");
            }
        }

        // =========================================================
        // HELPER: Tính điểm + Mức rủi ro dựa trên 3 hành vi phishing
        // =========================================================

        /// <returns>(isCorrect, scoreEarned, riskLevel)</returns>
        private static (bool isCorrect, int score, string riskLevel) CalculateScoreAndRisk(
            bool isPhishingScenario,
            bool isClickedLink,
            bool isCredentialLeaked,
            bool isReported,
            int baseScore)
        {
            if (isPhishingScenario)
            {
                // Kịch bản phishing thật
                if (isCredentialLeaked)
                    // Tệ nhất: nhập thông tin đăng nhập vào trang giả mạo
                    return (false, 0, "Critical");

                if (isClickedLink && !isReported)
                    // Bị lừa click link nhưng không báo cáo
                    return (false, 0, "High");

                if (isClickedLink && isReported)
                    // Báo cáo được nhưng đã lỡ click — partial credit
                    return (true, baseScore / 2, "Medium");

                if (isReported && !isClickedLink)
                    // Hoàn hảo: phát hiện và báo cáo mà không click
                    return (true, baseScore, "Low");

                // Không làm gì (không click, không báo cáo) — trung tính
                return (false, 0, "Medium");
            }
            else
            {
                // Kịch bản email an toàn (không phải phishing)
                if (isReported)
                    // Báo cáo nhầm email an toàn → false positive
                    return (false, 0, "Medium");

                // Không báo cáo email an toàn → xử lý đúng
                return (true, baseScore, "Low");
            }
        }

        public async Task<IEnumerable<UserCampaignAttemptResponse>> GetUserCampaignAttemptsAsync(int userId, int campaignId)
        {
            var attempts = await _unitOfWork.UserAttempts.GetAllAsync(
                x => x.UserId == userId && x.CampaignId == campaignId);

            return (attempts ?? Enumerable.Empty<UserAttempt>())
                .Select(a => new UserCampaignAttemptResponse
                {
                    AttemptId          = a.AttemptId,
                    ScenarioId         = a.ScenarioId,
                    IsCorrect          = a.IsCorrect,
                    Score              = a.Score,
                    IsClickedLink      = a.IsClickedLink,
                    IsCredentialLeaked = a.IsCredentialLeaked,
                    IsReported         = a.IsReported,
                    SubmittedAt        = a.SubmittedAt
                });
        }

        // =========================================================
        // LOGIC CÁC HÀM CRUD MỚI (ĐÃ CHUẨN HÓA TOÀN DIỆN)
        // =========================================================

        // 1. LẤY TẤT CẢ KỊCH BẢN (Nạp kèm thông tin bảng DifficultyLevel)
        public async Task<IEnumerable<ScenarioDetailResponse>> GetAllScenariosAsync()
        {
            var scenarios = await _unitOfWork.Scenarios.GetAllAsync(
                filter: null,
                include: query => query.Include(s => s.Difficulty).Include(s => s.Category),
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
                include: query => query.Include(s => s.Difficulty).Include(s => s.Category)
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

    }
}