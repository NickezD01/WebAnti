using System.Collections.Generic;

namespace AntiPhisher.Application.Request.LessonRequest
{
    public class SaveQuizRequest
    {
        public string Title     { get; set; } = "Kiểm tra nhanh";
        public int    PassScore { get; set; } = 70;
        public bool   IsActive  { get; set; } = true;
        public List<SaveQuizQuestionRequest> Questions { get; set; } = new();
    }

    public class SaveQuizQuestionRequest
    {
        public string QuestionText { get; set; } = null!;
        public int    QuestionType { get; set; } = 0;
        public int    OrderIndex   { get; set; }
        public List<SaveQuizOptionRequest> Options { get; set; } = new();
    }

    public class SaveQuizOptionRequest
    {
        public string OptionText { get; set; } = null!;
        public bool   IsCorrect  { get; set; }
        public int    OrderIndex { get; set; }
    }
}
