using AntiPhisher.Application.Request.LessonRequest;
using AntiPhisher.Application.Response.LessonResponse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AntiPhisher.Application.Interfaces
{
    public interface ILessonService
    {
        // Quản lý Bài học (Admin)
        Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request);
        Task<IEnumerable<LessonResponse>> GetAllLessonsAsync();
        Task<LessonResponse?> GetLessonByIdAsync(int lessonId);

        // Quản lý Tiến độ học tập (User)
        Task<UserLessonProgressResponse> TrackProgressAsync(int userId, UpdateLessonProgressRequest request);
        Task<IEnumerable<UserLessonProgressResponse>> GetUserProgressAsync(int callerUserId, string callerRole, int targetUserId);

        // Hàm kiểm tra cốt lõi: Xem User đã đủ điều kiện làm thực hành của Campaign chưa
        Task<bool> IsUserEligibleForCampaignAsync(int userId, int campaignId);
        Task SetCampaignPrerequisitesAsync(int campaignId, List<int> lessonIds);

        /// <summary>
        /// HÀM DÙNG CHUNG — Sinh UserLessonProgress cho campaign.
        /// Đọc prerequisites từ DB, dedup bằng HashSet, chỉ tạo record còn thiếu.
        /// specificUserId = null  → sync toàn bộ user được assign (dùng khi activate hoặc set-prerequisites).
        /// specificUserId = x     → chỉ sync user x (dùng khi invite-employee sau khi Vấn đề 3 chốt).
        /// </summary>
        Task SyncProgressForCampaignAsync(int campaignId, int? specificUserId = null);

        // PHẦN 2: Luồng phân phối bài học cho User
        /// <summary>Trả về các bài học bắt buộc từ tất cả Campaign đang hoạt động mà User được gán vào.</summary>
        Task<IEnumerable<MyLessonResponse>> GetMyLessonsAsync(int userId);
    }
}