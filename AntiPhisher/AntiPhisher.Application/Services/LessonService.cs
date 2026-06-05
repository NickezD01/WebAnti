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
                             LessonId = l.LessonId,
                             Title = l.Title,
                             Content = l.TheoryContent,
                             PhaseNumber = p.OrderIndex,
                             ModuleNumber = m.OrderIndex,
                             LessonOrder = m.OrderIndex + ((double)l.OrderIndex / 10),
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
            // 1. Tìm tất cả Campaign trực tiếp assign cho User
            var directAssignments = await _unitOfWork.CampaignUserAssignments.GetAllAsync(x => x.UserId == userId);
            var directCampaignIds = directAssignments?.Select(a => a.CampaignId).ToHashSet() ?? new HashSet<int>();

            // 2. Tìm Campaign gán qua Team
            var teamMemberships = await _unitOfWork.TeamMembers.GetAllAsync(x => x.UserId == userId);
            var userTeamIds = teamMemberships?.Select(tm => tm.TeamId).ToHashSet() ?? new HashSet<int>();

            if (userTeamIds.Any())
            {
                var teamAssignments = await _unitOfWork.CampaignTeamAssignments.GetAllAsync(
                    x => userTeamIds.Contains(x.TeamId));
                foreach (var ta in teamAssignments ?? new List<CampaignTeamAssignment>())
                    directCampaignIds.Add(ta.CampaignId);
            }

            if (!directCampaignIds.Any())
                return Enumerable.Empty<MyLessonResponse>();

            // 3. Lọc chỉ lấy Campaign đang Active và trong thời gian hiệu lực
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var activeCampaigns = await _unitOfWork.Campaigns.GetAllAsync(
                x => directCampaignIds.Contains(x.CampaignId) && x.IsActive);

            var validCampaigns = activeCampaigns?
                .Where(c =>
                    (c.StartDate == null || c.StartDate <= today) &&
                    (c.EndDate == null || c.EndDate >= today))
                .ToList() ?? new List<Campaign>();

            if (!validCampaigns.Any())
                return Enumerable.Empty<MyLessonResponse>();

            var validCampaignIds = validCampaigns.Select(c => c.CampaignId).ToHashSet();

            // 4. Lấy tất cả lesson bắt buộc từ các Campaign hợp lệ
            var allPrereqs = await _unitOfWork.CampaignPrerequisites.GetAllAsync(
                x => validCampaignIds.Contains(x.CampaignId));

            if (allPrereqs == null || !allPrereqs.Any())
                return Enumerable.Empty<MyLessonResponse>();

            // 5. Lấy tiến độ của User
            var requiredLessonIds = allPrereqs.Select(p => p.RequiredLessonId).Distinct().ToHashSet();
            var userProgress = await _unitOfWork.UserLessonProgresses.GetAllAsync(
                x => x.UserId == userId && requiredLessonIds.Contains(x.LessonId));
            var progressDict = userProgress?.ToDictionary(p => p.LessonId) ?? new Dictionary<int, UserLessonProgress>();

            // 6. Lấy thông tin chi tiết các Lesson
            var allLessons = await _unitOfWork.Lessons.GetAllAsync(
                x => requiredLessonIds.Contains(x.LessonId) && x.IsActive);
            var modules = await _unitOfWork.Modules.GetAllAsync(null);
            var phases = await _unitOfWork.Phases.GetAllAsync(null);

            var moduleDict = modules?.ToDictionary(m => m.ModuleId) ?? new Dictionary<int, Module>();
            var phaseDict = phases?.ToDictionary(p => p.PhaseId) ?? new Dictionary<int, Phase>();

            // 7. Build response — mỗi lesson kèm trạng thái và campaign nguồn
            var result = new List<MyLessonResponse>();

            foreach (var prereq in allPrereqs)
            {
                var lesson = allLessons?.FirstOrDefault(l => l.LessonId == prereq.RequiredLessonId);
                if (lesson == null) continue;

                // Tránh trùng lặp cùng lesson từ nhiều campaign: chỉ lấy lần đầu tiên
                if (result.Any(r => r.LessonId == lesson.LessonId)) continue;

                var campaign = validCampaigns.FirstOrDefault(c => c.CampaignId == prereq.CampaignId);

                moduleDict.TryGetValue(lesson.ModuleId, out var module);
                int phaseNum = module != null && phaseDict.TryGetValue(module.PhaseId, out var ph) ? ph.OrderIndex : 0;
                int moduleNum = module?.OrderIndex ?? 0;

                progressDict.TryGetValue(lesson.LessonId, out var progress);
                string status = progress == null ? "NotStarted"
                    : progress.IsCompleted ? "Completed"
                    : "InProgress";

                result.Add(new MyLessonResponse
                {
                    LessonId = lesson.LessonId,
                    Title = lesson.Title,
                    Content = lesson.TheoryContent,
                    PhaseNumber = phaseNum,
                    ModuleNumber = moduleNum,
                    LessonOrder = moduleNum + ((double)lesson.OrderIndex / 10),
                    EstimatedMinutes = 15,
                    Status = status,
                    CompletedAt = progress?.CompletedAt,
                    CampaignId = prereq.CampaignId,
                    CampaignName = campaign?.CampaignName ?? string.Empty
                });
            }

            return result.OrderBy(x => x.PhaseNumber).ThenBy(x => x.LessonOrder);
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
    }
}