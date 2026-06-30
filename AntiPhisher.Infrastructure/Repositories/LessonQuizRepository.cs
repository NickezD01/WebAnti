using AntiPhisher.Application.Repository;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Infrastructure.Repositories
{
    public class LessonQuizRepository   : GenericRepository<LessonQuiz>,   ILessonQuizRepository   { public LessonQuizRepository(AppDbContext c)   : base(c) { } }
    public class QuizQuestionRepository : GenericRepository<QuizQuestion>,  IQuizQuestionRepository { public QuizQuestionRepository(AppDbContext c)  : base(c) { } }
    public class QuizOptionRepository   : GenericRepository<QuizOption>,    IQuizOptionRepository   { public QuizOptionRepository(AppDbContext c)    : base(c) { } }
}
