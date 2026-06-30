using AntiPhisher.Application.Features;
using AntiPhisher.Domain.Models;

namespace AntiPhisher.Application.Repository
{
    public interface ILessonQuizRepository    : IGenericRepository<LessonQuiz>    { }
    public interface IQuizQuestionRepository  : IGenericRepository<QuizQuestion>  { }
    public interface IQuizOptionRepository    : IGenericRepository<QuizOption>    { }
}
