using System.Collections.Generic;

namespace AntiPhisher.Domain.Models
{
    public class LessonQuiz
    {
        public int QuizId     { get; set; }
        public int LessonId   { get; set; }
        public string Title   { get; set; } = "Kiểm tra nhanh";
        public int PassScore  { get; set; } = 70;   // phần trăm đạt
        public bool IsActive  { get; set; } = true;

        public virtual Lesson Lesson { get; set; } = null!;
        public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }

    public class QuizQuestion
    {
        public int    QuestionId   { get; set; }
        public int    QuizId       { get; set; }
        public string QuestionText { get; set; } = null!;
        public int    QuestionType { get; set; } = 0;   // 0=SingleChoice 1=MultipleChoice
        public int    OrderIndex   { get; set; }

        public virtual LessonQuiz Quiz { get; set; } = null!;
        public virtual ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
    }

    public class QuizOption
    {
        public int    OptionId   { get; set; }
        public int    QuestionId { get; set; }
        public string OptionText { get; set; } = null!;
        public bool   IsCorrect  { get; set; }
        public int    OrderIndex { get; set; }

        public virtual QuizQuestion Question { get; set; } = null!;
    }
}
