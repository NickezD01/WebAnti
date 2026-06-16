using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.LessonRequest;
using AntiPhisher.Application.Response.LessonResponse;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyLessonResponse = AntiPhisher.Application.Response.LessonResponse.MyLessonResponse;

namespace AntiPhisher.Application.Services
{
    public class LessonService : ILessonService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LessonService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // =========================================================================
        // 1. QUẢN LÝ BÀI HỌC (ADMIN) - CREATE LESSON (Đã fix lỗi request.Content)
        // =========================================================================
        public async Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request)
        {
            // 1. Kiểm tra trực tiếp xem ModuleId truyền lên có tồn tại không
            var module = await _unitOfWork.Modules.GetAsync(x => x.ModuleId == request.ModuleId);
            if (module == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy Module với ID {request.ModuleId}");
            }

            // 2. Lấy thông tin Phase để phục vụ việc map dữ liệu phẳng ra LessonResponse
            var phase = await _unitOfWork.Phases.GetAsync(x => x.PhaseId == module.PhaseId);

            // 3. Tự động tính thứ tự bài học nếu Admin không truyền hoặc truyền <= 0
            int orderIndex = request.OrderIndex;
            if (orderIndex <= 0)
            {
                var existingLessons = await _unitOfWork.Lessons.GetAllAsync(x => x.ModuleId == request.ModuleId);
                orderIndex = (existingLessons?.Count ?? 0) + 1;
            }

            // 4. Khởi tạo thực thể Lesson chuẩn theo cấu trúc database gốc
            var newLesson = new Lesson
            {
                ModuleId = request.ModuleId,
                Title = request.Title,
                TheoryContent = request.Content ?? string.Empty, // ĐÃ SỬA: Dùng request.Content thay vì request.TheoryContent
                SimulationGuide = string.Empty,
                OrderIndex = orderIndex,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Lessons.AddAsync(newLesson);
            await _unitOfWork.SaveChangeAsync();

            // 5. Trả về đúng định dạng phẳng DTO LessonResponse của bạn
            int phaseNum = phase != null ? phase.OrderIndex : 0;
            int moduleNum = module.OrderIndex;

            return new LessonResponse
            {
                LessonId = newLesson.LessonId,
                Title = newLesson.Title,
                Content = newLesson.TheoryContent,
                PhaseNumber = phaseNum,
                ModuleNumber = moduleNum,
                // Tính toán LessonOrder dạng số thực (Ví dụ: Module 1, Bài số 2 -> 1.2) cho khớp DTO Response của bạn
                LessonOrder = moduleNum + ((double)newLesson.OrderIndex / 10),
                EstimatedMinutes = request.EstimatedMinutes ?? 15
            };
        }

        // =========================================================================
        // 1b. QUẢN LÝ BÀI HỌC (ADMIN) - UPDATE LESSON
        // =========================================================================
        public async Task<LessonResponse> UpdateLessonAsync(int lessonId, UpdateLessonRequest request)
        {
            var l = await _unitOfWork.Lessons.GetAsync(x => x.LessonId == lessonId);
            if (l == null) throw new KeyNotFoundException($"Không tìm thấy bài học ID {lessonId}");

            if (request.Title != null)          l.Title           = request.Title.Trim();
            if (request.Content != null)        l.TheoryContent   = request.Content;
            if (request.SimulationGuide != null) l.SimulationGuide = request.SimulationGuide;
            l.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangeAsync();

            var m = await _unitOfWork.Modules.GetAsync(x => x.ModuleId == l.ModuleId);
            var p = m != null ? await _unitOfWork.Phases.GetAsync(x => x.PhaseId == m.PhaseId) : null;

            return new LessonResponse
            {
                LessonId      = l.LessonId,
                Title         = l.Title,
                Content       = l.TheoryContent,
                SimulationGuide = l.SimulationGuide,
                PhaseNumber   = p?.OrderIndex ?? 0,
                PhaseName     = p?.PhaseName,
                ModuleNumber  = m?.OrderIndex ?? 0,
                ModuleName    = m?.ModuleName,
                LessonOrder   = (m?.OrderIndex ?? 0) + ((double)l.OrderIndex / 10),
                EstimatedMinutes = 15,
            };
        }

        // =========================================================================
        // 2. QUẢN LÝ BÀI HỌC (ADMIN) - GET ALL LESSONS
        // =========================================================================
        public async Task<IEnumerable<LessonResponse>> GetAllLessonsAsync()
        {
            var lessons = await _unitOfWork.Lessons.GetAllAsync(null);
            var modules = await _unitOfWork.Modules.GetAllAsync(null);
            var phases = await _unitOfWork.Phases.GetAllAsync(null);

            if (lessons == null) return new List<LessonResponse>();

            var result = from l in lessons
                         join m in modules on l.ModuleId equals m.ModuleId
                         join p in phases on m.PhaseId equals p.PhaseId
                         select new LessonResponse
                         {
                             LessonId        = l.LessonId,
                             Title           = l.Title,
                             Content         = l.TheoryContent,
                             SimulationGuide = l.SimulationGuide,
                             PhaseNumber     = p.OrderIndex,
                             PhaseName       = p.PhaseName,
                             ModuleNumber    = m.OrderIndex,
                             ModuleName      = m.ModuleName,
                             LessonOrder     = m.OrderIndex + ((double)l.OrderIndex / 10),
                             EstimatedMinutes = 15
                         };

            return result.OrderBy(x => x.PhaseNumber)
                         .ThenBy(x => x.ModuleNumber)
                         .ThenBy(x => x.LessonOrder)
                         .ToList();
        }

        // =========================================================================
        // 3. QUẢN LÝ BÀI HỌC (ADMIN) - GET LESSON BY ID
        // =========================================================================
        public async Task<LessonResponse?> GetLessonByIdAsync(int lessonId)
        {
            var l = await _unitOfWork.Lessons.GetAsync(x => x.LessonId == lessonId);
            if (l == null) return null;

            var m = await _unitOfWork.Modules.GetAsync(x => x.ModuleId == l.ModuleId);
            var p = m != null ? await _unitOfWork.Phases.GetAsync(x => x.PhaseId == m.PhaseId) : null;

            int pNum = p != null ? p.OrderIndex : 0;
            int mNum = m != null ? m.OrderIndex : 0;

            return new LessonResponse
            {
                LessonId = l.LessonId,
                Title = l.Title,
                Content = l.TheoryContent,
                PhaseNumber = pNum,
                ModuleNumber = mNum,
                LessonOrder = mNum + ((double)l.OrderIndex / 10),
                EstimatedMinutes = 15
            };
        }

        // =========================================================================
        // 4. QUẢN LÝ TIẾN ĐỘ HỌC TẬP (USER) - TRACK PROGRESS
        // =========================================================================
        public async Task<UserLessonProgressResponse> TrackProgressAsync(int userId, UpdateLessonProgressRequest request)
        {
            var lesson = await _unitOfWork.Lessons.GetAsync(x => x.LessonId == request.LessonId);
            if (lesson == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bài học lý thuyết với ID {request.LessonId}");
            }

            var progressList = await _unitOfWork.UserLessonProgresses.GetAllAsync(x => x.UserId == userId && x.LessonId == request.LessonId);
            var progress = progressList.FirstOrDefault();

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = request.LessonId,
                    IsCompleted = request.IsCompleted,
                    CompletedAt = request.IsCompleted ? DateTime.UtcNow : null
                };
                await _unitOfWork.UserLessonProgresses.AddAsync(progress);
            }
            else
            {
                if (progress.IsCompleted != request.IsCompleted)
                {
                    progress.IsCompleted = request.IsCompleted;
                    progress.CompletedAt = request.IsCompleted ? DateTime.UtcNow : null;
                }
            }

            await _unitOfWork.SaveChangeAsync();

            return new UserLessonProgressResponse
            {
                ProgressId = progress.ProgressId,
                UserId = progress.UserId,
                LessonId = progress.LessonId,
                IsCompleted = progress.IsCompleted,
                CompletedAt = progress.CompletedAt
            };
        }

        // =========================================================================
        // 5. QUẢN LÝ TIẾN ĐỘ HỌC TẬP (USER) - GET USER PROGRESS
        // =========================================================================
        public async Task<IEnumerable<UserLessonProgressResponse>> GetUserProgressAsync(int callerUserId, string callerRole, int targetUserId)
        {
            // Self-access always allowed
            if (callerUserId != targetUserId)
            {
                if (callerRole == "Admin")
                {
                    // Admin can see any user — no extra check
                }
                else if (callerRole == "Manager")
                {
                    var manager = await _unitOfWork.Users.GetAsync(x => x.UserId == callerUserId);
                    var targetUser = await _unitOfWork.Users.GetAsync(x => x.UserId == targetUserId);

                    if (targetUser == null)
                        throw new KeyNotFoundException($"Không tìm thấy user với ID {targetUserId}");

                    if (manager?.CompanyId == null || targetUser.CompanyId == null
                        || manager.CompanyId != targetUser.CompanyId)
                        throw new UnauthorizedAccessException("Không có quyền xem tiến độ học tập của nhân viên công ty khác.");
                }
                else
                {
                    throw new UnauthorizedAccessException("Không có quyền truy cập.");
                }
            }

            var progressList = await _unitOfWork.UserLessonProgresses.GetAllAsync(x => x.UserId == targetUserId);
            if (progressList == null) return new List<UserLessonProgressResponse>();

            return progressList.Select(p => new UserLessonProgressResponse
            {
                ProgressId = p.ProgressId,
                UserId = p.UserId,
                LessonId = p.LessonId,
                IsCompleted = p.IsCompleted,
                CompletedAt = p.CompletedAt
            }).ToList();
        }

        // =========================================================================
        // 6. HÀM KIỂM TRA CỐT LÕI: Xem User đã đủ điều kiện làm thực hành Campaign chưa
        // =========================================================================
        public async Task<bool> IsUserEligibleForCampaignAsync(int userId, int campaignId)
        {
            var campaign = await _unitOfWork.Campaigns.GetAsync(x => x.CampaignId == campaignId);
            if (campaign == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy chiến dịch với ID {campaignId}");
            }

            var allPrereqs = await _unitOfWork.CampaignPrerequisites.GetAllAsync(null);

            var requiredLessonIds = allPrereqs
                .Where(x => x.CampaignId == campaignId)
                .Select(x => x.RequiredLessonId)
                .ToList();

            if (!requiredLessonIds.Any())
            {
                return true;
            }

            var userProgress = await _unitOfWork.UserLessonProgresses.GetAllAsync(x => x.UserId == userId && x.IsCompleted);
            var completedLessonIds = userProgress.Select(x => x.LessonId).ToList();

            var hasMissingLessons = requiredLessonIds.Except(completedLessonIds).Any();

            return !hasMissingLessons;
        }

        public async Task SetCampaignPrerequisitesAsync(int campaignId, List<int> lessonIds)
        {
            // 1. Xóa các điều kiện cũ
            var existingPrereqs = await _unitOfWork.CampaignPrerequisites.GetAllAsync(x => x.CampaignId == campaignId);
            foreach (var item in existingPrereqs)
                _unitOfWork.CampaignPrerequisites.Remove(item);

            // 2. Thêm mới danh sách điều kiện
            var newPrereqs = lessonIds.Select(lessonId => new CampaignPrerequisite
            {
                CampaignId = campaignId,
                RequiredLessonId = lessonId
            }).ToList();

            await _unitOfWork.CampaignPrerequisites.AddRangeAsync(newPrereqs);
            await _unitOfWork.SaveChangeAsync();

            // 3. TRIGGER: Dùng hàm dùng chung — đọc prerequisites từ DB (vừa commit ở trên)
            await SyncProgressForCampaignAsync(campaignId);
        }

        // =========================================================================
        // PHẦN 2 — Luồng phân phối bài học: GetMyLessons
        // =========================================================================

        public async Task<IEnumerable<MyLessonResponse>> GetMyLessonsAsync(int userId)
        {
            var isUnlocked = await IsUserUnlockedAsync(userId);

            var phases  = await _unitOfWork.Phases.GetAllAsync(p => p.IsActive);
            var modules = await _unitOfWork.Modules.GetAllAsync(m => m.IsActive);
            var lessons = await _unitOfWork.Lessons.GetAllAsync(l => l.IsActive);

            if (lessons == null || !lessons.Any())
                return Enumerable.Empty<MyLessonResponse>();

            var phaseDict  = phases?.ToDictionary(p => p.PhaseId)   ?? new Dictionary<int, Phase>();
            var moduleDict = modules?.ToDictionary(m => m.ModuleId)  ?? new Dictionary<int, Module>();

            var lessonIds = lessons.Select(l => l.LessonId).ToHashSet();
            var progList  = await _unitOfWork.UserLessonProgresses.GetAllAsync(
                p => p.UserId == userId && lessonIds.Contains(p.LessonId));
            var progDict  = progList?.ToDictionary(p => p.LessonId) ?? new Dictionary<int, UserLessonProgress>();

            var result = new List<MyLessonResponse>();
            foreach (var lesson in lessons)
            {
                moduleDict.TryGetValue(lesson.ModuleId, out var mod);
                int phaseNum  = mod != null && phaseDict.TryGetValue(mod.PhaseId, out var ph) ? ph.OrderIndex : 0;
                int moduleNum = mod?.OrderIndex ?? 0;

                progDict.TryGetValue(lesson.LessonId, out var prog);

                result.Add(new MyLessonResponse
                {
                    LessonId         = lesson.LessonId,
                    Title            = lesson.Title,
                    Content          = lesson.TheoryContent,
                    PhaseNumber      = phaseNum,
                    ModuleNumber     = moduleNum,
                    LessonOrder      = moduleNum + ((double)lesson.OrderIndex / 10),
                    EstimatedMinutes = 15,
                    Status           = prog == null ? "NotStarted" : prog.IsCompleted ? "Completed" : "InProgress",
                    CompletedAt      = prog?.CompletedAt,
                    CampaignId       = 0,
                    CampaignName     = phaseNum == 1 ? "Học miễn phí" : string.Empty,
                    IsLocked         = !isUnlocked && phaseNum != 1
                });
            }

            return result.OrderBy(x => x.PhaseNumber).ThenBy(x => x.LessonOrder);
        }

        public async Task<bool> IsUserUnlockedAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetAsync(u => u.UserId == userId);
            if (user == null) return false;

            if (user.CompanyId == null)
            {
                // Individual B2C: subscription belongs to the user directly (CompanyId = null)
                var sub = await _unitOfWork.Subscriptions.GetAsync(
                    s => s.AccountId == userId && s.CompanyId == null &&
                         s.Status == SubscriptionStatus.Active &&
                         s.EndDate > DateTime.UtcNow);
                return sub != null;
            }
            else
            {
                // Company B2B: subscription belongs to the company
                var sub = await _unitOfWork.Subscriptions.GetAsync(
                    s => s.CompanyId == user.CompanyId &&
                         s.Status == SubscriptionStatus.Active &&
                         s.EndDate > DateTime.UtcNow);
                return sub != null;
            }
        }

        private async Task<List<MyLessonResponse>> BuildPhase1LessonsAsync(int userId)
        {
            var phase1 = await _unitOfWork.Phases.GetAsync(p => p.OrderIndex == 1 && p.IsActive);
            if (phase1 == null) return new List<MyLessonResponse>();

            var mods = await _unitOfWork.Modules.GetAllAsync(m => m.PhaseId == phase1.PhaseId && m.IsActive);
            var modIds = mods?.Select(m => m.ModuleId).ToHashSet() ?? new HashSet<int>();
            if (!modIds.Any()) return new List<MyLessonResponse>();

            var lessons = await _unitOfWork.Lessons.GetAllAsync(l => modIds.Contains(l.ModuleId) && l.IsActive);
            var lessonIds = lessons?.Select(l => l.LessonId).ToHashSet() ?? new HashSet<int>();
            if (!lessonIds.Any()) return new List<MyLessonResponse>();

            var progList = await _unitOfWork.UserLessonProgresses.GetAllAsync(
                p => p.UserId == userId && lessonIds.Contains(p.LessonId));
            var progDict = progList?.ToDictionary(p => p.LessonId) ?? new Dictionary<int, UserLessonProgress>();
            var modDict  = mods?.ToDictionary(m => m.ModuleId) ?? new Dictionary<int, Module>();

            var output = new List<MyLessonResponse>();
            foreach (var lesson in lessons ?? Enumerable.Empty<Lesson>())
            {
                modDict.TryGetValue(lesson.ModuleId, out var mod);
                progDict.TryGetValue(lesson.LessonId, out var prog);
                output.Add(new MyLessonResponse
                {
                    LessonId         = lesson.LessonId,
                    Title            = lesson.Title,
                    Content          = lesson.TheoryContent,
                    PhaseNumber      = phase1.OrderIndex,
                    ModuleNumber     = mod?.OrderIndex ?? 0,
                    LessonOrder      = (mod?.OrderIndex ?? 0) + ((double)lesson.OrderIndex / 10),
                    EstimatedMinutes = 15,
                    Status           = prog == null ? "NotStarted" : prog.IsCompleted ? "Completed" : "InProgress",
                    CompletedAt      = prog?.CompletedAt,
                    CampaignId       = 0,
                    CampaignName     = "Học miễn phí",
                    IsLocked         = false
                });
            }
            return output;
        }

        // =========================================================================
        // HÀM DÙNG CHUNG — Sinh UserLessonProgress, được gọi từ:
        //   • SetCampaignPrerequisitesAsync (sau khi save prereqs vào DB)
        //   • CampaignService.ActivateCampaignAsync (khi IsActive false→true)
        //   • Tương lai: invite-employee (specificUserId != null) — xử lý ở Vấn đề 3
        // =========================================================================
        public async Task SyncProgressForCampaignAsync(int campaignId, int? specificUserId = null)
        {
            // 1. Lấy lesson IDs từ DB (không nhận tham số — luôn đọc từ DB để nhất quán)
            var prereqs = await _unitOfWork.CampaignPrerequisites.GetAllAsync(x => x.CampaignId == campaignId);
            var lessonIds = prereqs?.Select(p => p.RequiredLessonId).Distinct().ToList() ?? new List<int>();
            if (!lessonIds.Any()) return;

            // 2. Lấy user IDs được assign vào campaign (direct + via team)
            var userIds = new HashSet<int>();

            if (specificUserId.HasValue)
            {
                // Chỉ sync cho 1 user cụ thể (use case: invite-employee)
                userIds.Add(specificUserId.Value);
            }
            else
            {
                // Sync toàn bộ assigned users (use case: activate campaign hoặc set-prerequisites)
                var directUsers = await _unitOfWork.CampaignUserAssignments.GetAllAsync(x => x.CampaignId == campaignId);
                foreach (var a in directUsers ?? new List<CampaignUserAssignment>())
                    userIds.Add(a.UserId);

                var teamAssignments = await _unitOfWork.CampaignTeamAssignments.GetAllAsync(x => x.CampaignId == campaignId);
                if (teamAssignments?.Any() == true)
                {
                    var teamIds = teamAssignments.Select(t => t.TeamId).ToHashSet();
                    var teamMembers = await _unitOfWork.TeamMembers.GetAllAsync(x => teamIds.Contains(x.TeamId));
                    foreach (var m in teamMembers ?? new List<TeamMember>())
                        userIds.Add(m.UserId);
                }
            }

            if (!userIds.Any()) return;

            // 3. Dedup: chỉ tạo record chưa tồn tại
            var existing = await _unitOfWork.UserLessonProgresses.GetAllAsync(
                x => userIds.Contains(x.UserId) && lessonIds.Contains(x.LessonId));
            var existingKeys = existing?.Select(p => (p.UserId, p.LessonId)).ToHashSet()
                              ?? new HashSet<(int, int)>();

            var newRecords = new List<UserLessonProgress>();
            foreach (var uid in userIds)
                foreach (var lid in lessonIds)
                    if (!existingKeys.Contains((uid, lid)))
                        newRecords.Add(new UserLessonProgress
                        {
                            UserId = uid,
                            LessonId = lid,
                            IsCompleted = false,
                            CompletedAt = null
                        });

            if (newRecords.Any())
            {
                await _unitOfWork.UserLessonProgresses.AddRangeAsync(newRecords);
                await _unitOfWork.SaveChangeAsync();
            }
        }

        // =========================================================================
        // QUIZ — lấy quiz của 1 bài học
        // =========================================================================
        public async Task<QuizResponse?> GetQuizByLessonIdAsync(int lessonId)
        {
            var quiz = await _unitOfWork.LessonQuizzes.GetAsync(q => q.LessonId == lessonId);
            if (quiz == null) return null;

            var questions = await _unitOfWork.QuizQuestions.GetAllAsync(q => q.QuizId == quiz.QuizId);
            var questionIds = questions.Select(q => q.QuestionId).ToList();
            var allOptions = await _unitOfWork.QuizOptions.GetAllAsync(o => questionIds.Contains(o.QuestionId));

            return MapQuizResponse(quiz, questions, allOptions);
        }

        // =========================================================================
        // QUIZ — lưu toàn bộ quiz (upsert: xóa cũ + tạo mới)
        // =========================================================================
        public async Task<QuizResponse> SaveQuizAsync(int lessonId, SaveQuizRequest request)
        {
            var lesson = await _unitOfWork.Lessons.GetAsync(l => l.LessonId == lessonId);
            if (lesson == null) throw new KeyNotFoundException($"Không tìm thấy bài học ID {lessonId}");

            // Xóa quiz cũ nếu có
            var existingQuiz = await _unitOfWork.LessonQuizzes.GetAsync(q => q.LessonId == lessonId);
            if (existingQuiz != null)
            {
                var oldQuestions = await _unitOfWork.QuizQuestions.GetAllAsync(q => q.QuizId == existingQuiz.QuizId);
                var oldQIds = oldQuestions.Select(q => q.QuestionId).ToList();
                var oldOptions = await _unitOfWork.QuizOptions.GetAllAsync(o => oldQIds.Contains(o.QuestionId));

                foreach (var opt in oldOptions)  _unitOfWork.QuizOptions.Remove(opt);
                foreach (var q in oldQuestions)  _unitOfWork.QuizQuestions.Remove(q);
                _unitOfWork.LessonQuizzes.Remove(existingQuiz);
                await _unitOfWork.SaveChangeAsync();
            }

            // Tạo quiz mới
            var quiz = new LessonQuiz
            {
                LessonId  = lessonId,
                Title     = request.Title?.Trim() ?? "Kiểm tra nhanh",
                PassScore = request.PassScore,
                IsActive  = request.IsActive
            };
            await _unitOfWork.LessonQuizzes.AddAsync(quiz);
            await _unitOfWork.SaveChangeAsync();

            // Tạo câu hỏi và đáp án
            var newQuestions = new List<QuizQuestion>();
            for (int qi = 0; qi < request.Questions.Count; qi++)
            {
                var qReq = request.Questions[qi];
                var question = new QuizQuestion
                {
                    QuizId       = quiz.QuizId,
                    QuestionText = qReq.QuestionText.Trim(),
                    QuestionType = qReq.QuestionType,
                    OrderIndex   = qReq.OrderIndex > 0 ? qReq.OrderIndex : qi + 1
                };
                await _unitOfWork.QuizQuestions.AddAsync(question);
                await _unitOfWork.SaveChangeAsync();

                for (int oi = 0; oi < qReq.Options.Count; oi++)
                {
                    var oReq = qReq.Options[oi];
                    var option = new QuizOption
                    {
                        QuestionId = question.QuestionId,
                        OptionText = oReq.OptionText.Trim(),
                        IsCorrect  = oReq.IsCorrect,
                        OrderIndex = oReq.OrderIndex > 0 ? oReq.OrderIndex : oi + 1
                    };
                    await _unitOfWork.QuizOptions.AddAsync(option);
                }
                await _unitOfWork.SaveChangeAsync();
                newQuestions.Add(question);
            }

            var savedOptions = await _unitOfWork.QuizOptions.GetAllAsync(
                o => newQuestions.Select(q => q.QuestionId).Contains(o.QuestionId));

            return MapQuizResponse(quiz, newQuestions, savedOptions);
        }

        private static QuizResponse MapQuizResponse(LessonQuiz quiz, List<QuizQuestion> questions, List<QuizOption> options)
        {
            return new QuizResponse
            {
                QuizId    = quiz.QuizId,
                LessonId  = quiz.LessonId,
                Title     = quiz.Title,
                PassScore = quiz.PassScore,
                IsActive  = quiz.IsActive,
                Questions = questions.OrderBy(q => q.OrderIndex).Select(q => new QuizQuestionResponse
                {
                    QuestionId   = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    OrderIndex   = q.OrderIndex,
                    Options      = options.Where(o => o.QuestionId == q.QuestionId)
                                          .OrderBy(o => o.OrderIndex)
                                          .Select(o => new QuizOptionResponse
                                          {
                                              OptionId   = o.OptionId,
                                              OptionText = o.OptionText,
                                              IsCorrect  = o.IsCorrect,
                                              OrderIndex = o.OrderIndex
                                          }).ToList()
                }).ToList()
            };
        }
    }
}