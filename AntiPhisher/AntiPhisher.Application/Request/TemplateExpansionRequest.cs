namespace AntiPhisher.Application.Request.TemplateExpansion
{
    public class TemplateExpansionRequest
    {
        public int DifficultyId { get; set; }
        public int? CategoryId { get; set; }   // tuỳ chọn — lọc thêm theo category nếu Admin muốn
        public int FewShotCount { get; set; } = 3;
    }
}