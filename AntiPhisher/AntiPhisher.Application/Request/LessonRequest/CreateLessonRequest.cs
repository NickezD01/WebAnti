using System;

namespace AntiPhisher.Application.Request.LessonRequest
{
    public class CreateLessonRequest
    {
        public int ModuleId { get; set; }

        public string Title { get; set; } = null!;

        public string? Content { get; set; } // Nội dung bài học lý thuyết

        public int OrderIndex { get; set; } // Thứ tự bài học dạng số nguyên (1, 2, 3...)

        public int? EstimatedMinutes { get; set; }
    }
}