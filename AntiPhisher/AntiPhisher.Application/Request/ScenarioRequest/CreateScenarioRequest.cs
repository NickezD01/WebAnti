namespace AntiPhisher.Application.Request.ScenarioRequest
{
    public class CreateScenarioRequest
    {
        public string Title { get; set; } = null!; // Tiêu đề kịch bản (Ví dụ: Thực hành nhận diện Email quà tặng)
        public string Description { get; set; } = null!; // Mô tả yêu cầu (Ví dụ: Hãy kiểm tra email sau...)
        public string Subject { get; set; } = null!; // Tiêu đề Email nhử
        public string SenderName { get; set; } = null!; // Tên người gửi hiển thị (Ví dụ: Cục Thuế, Google Support)
        public string SenderEmail { get; set; } = null!; // Email người gửi mạo danh
        public string RecipientName { get; set; } = null!; // Tên người nhận (Học viên hoặc tên giả định)
        public string EmailBodyHtml { get; set; } = null!; // Nội dung Email dạng HTML để hiển thị giao diện Mail
        public bool IsPhishing { get; set; } // Có phải link lừa đảo thật không hay email an toàn
        public string? PhishingIndicators { get; set; } // Các dấu hiệu lừa đảo (Dạng JSON hoặc Text để khoanh đỏ vùng lỗi)
        public string ExplanationHint { get; set; } = null!; // Lời giải thích khi học viên làm xong
        public int CategoryId { get; set; } // Thuộc danh mục nào (Email, Tin nhắn, Social...)
        public int DifficultyId { get; set; } // Độ khó (Cơ bản, Trung bình, Nâng cao theo Phase)
    }
}