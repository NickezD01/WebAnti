using System.Collections.Generic;

namespace AntiPhisher.Application.Response.LessonResponse
{
    public class QuizResponse
    {
        public int    QuizId    { get; set; }
        public int    LessonId  { get; set; }
        public string Title     { get; set; } = null!;
        public int    PassScore { get; set; }
        public bool   IsActive  { get; set; }
        public List<QuizQuestionResponse> Questions { get; set; } = new();
    }

    public class QuizQuestionResponse
    {
        public int    QuestionId   { get; set; }
        public string QuestionText { get; set; } = null!;
        public int    QuestionType { get; set; }
        public int    OrderIndex   { get; set; }
        public List<QuizOptionResponse> Options { get; set; } = new();
    }

    public class QuizOptionResponse
    {
        public int    OptionId   { get; set; }
        public string OptionText { get; set; } = null!;
        public bool   IsCorrect  { get; set; }
        public int    OrderIndex { get; set; }
    }
}
