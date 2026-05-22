using AntiPhisher.Application.Interfaces;
using AntiPhisher.Application.Request.LessonRequest;
using AntiPhisher.Application.Response.LessonResponse;
using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public async Task<UserLessonProgressResponse> TrackProgressAsync(UpdateLessonProgressRequest request)
        {
            var lesson = await _unitOfWork.Lessons.GetAsync(x => x.LessonId == request.LessonId);
            if (lesson == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy bài học lý thuyết với ID {request.LessonId}");
            }

            var progressList = await _unitOfWork.UserLessonProgresses.GetAllAsync(x => x.UserId == request.UserId && x.LessonId == request.LessonId);
            var progress = progressList.FirstOrDefault();

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = request.UserId,
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
        public async Task<IEnumerable<UserLessonProgressResponse>> GetUserProgressAsync(int userId)
        {
            var progressList = await _unitOfWork.UserLessonProgresses.GetAllAsync(x => x.UserId == userId);
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
            // 1. Lấy danh sách các điều kiện cũ
            var existingPrereqs = await _unitOfWork.CampaignPrerequisites.GetAllAsync(x => x.CampaignId == campaignId);

            // 2. Xóa các bản ghi cũ
            foreach (var item in existingPrereqs)
            {
                // Gọi phương thức đồng bộ Remove() thay vì RemoveAsync()
                _unitOfWork.CampaignPrerequisites.Remove(item);
            }

            // 3. Thêm mới danh sách điều kiện
            var newPrereqs = lessonIds.Select(lessonId => new CampaignPrerequisite
            {
                CampaignId = campaignId,
                RequiredLessonId = lessonId
            }).ToList();

            await _unitOfWork.CampaignPrerequisites.AddRangeAsync(newPrereqs);

            // 4. Lưu thay đổi
            await _unitOfWork.SaveChangeAsync();
        }
    }
}