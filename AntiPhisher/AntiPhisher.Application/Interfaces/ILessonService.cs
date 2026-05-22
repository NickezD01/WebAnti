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
        Task<UserLessonProgressResponse> TrackProgressAsync(UpdateLessonProgressRequest request);
        Task<IEnumerable<UserLessonProgressResponse>> GetUserProgressAsync(int userId);

        // Hàm kiểm tra cốt lõi: Xem User đã đủ điều kiện làm thực hành của Campaign chưa
        Task<bool> IsUserEligibleForCampaignAsync(int userId, int campaignId);
        Task SetCampaignPrerequisitesAsync(int campaignId, List<int> lessonIds);
    }
}