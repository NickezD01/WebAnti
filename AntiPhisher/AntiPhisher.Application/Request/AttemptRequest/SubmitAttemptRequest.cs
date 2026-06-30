using System.ComponentModel.DataAnnotations;

namespace AntiPhisher.Application.Request.AttemptRequest
{
    /// <summary>
    /// CHANGED: Thay trường UserAnswer (string "Safe"/"Phishing") bằng 3 boolean
    /// mô tả hành vi thực tế của User khi tiếp xúc email phishing.
    /// </summary>
    public class SubmitAttemptRequest
    {
        [Required]
        public int ScenarioId { get; set; }

        public int? CampaignId { get; set; }

        public int TimeTakenSeconds { get; set; }

        // ===================================================
        // NEW: 3 trường hành vi phishing
        // ===================================================

        /// <summary>User đã bấm vào link/button giả mạo trong email.</summary>
        public bool IsClickedLink { get; set; } = false;

        /// <summary>User đã nhập thông tin đăng nhập / thẻ tín dụng vào trang giả mạo.</summary>
        public bool IsCredentialLeaked { get; set; } = false;

        /// <summary>User đã nhấn nút "Báo cáo lừa đảo" (hành vi đúng nhất với phishing).</summary>
        public bool IsReported { get; set; } = false;

        /// <summary>Nhận xét tự do của user: dấu hiệu nhận ra, thủ thuật tâm lý, loại tấn công — truyền vào AI để đánh giá.</summary>
        public string? UserObservations { get; set; }
    }
}
