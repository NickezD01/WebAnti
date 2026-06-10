using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class LessonQuizConfiguration : IEntityTypeConfiguration<LessonQuiz>
    {
        public void Configure(EntityTypeBuilder<LessonQuiz> builder)
        {
            builder.ToTable("LessonQuizzes");
            builder.HasKey(e => e.QuizId);
            builder.Property(e => e.Title).IsRequired().HasMaxLength(255);
            builder.HasOne(e => e.Lesson)
                .WithMany()
                .HasForeignKey(e => e.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
    {
        public void Configure(EntityTypeBuilder<QuizQuestion> builder)
        {
            builder.ToTable("QuizQuestions");
            builder.HasKey(e => e.QuestionId);
            builder.Property(e => e.QuestionText).IsRequired();
            builder.HasOne(e => e.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(e => e.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
    {
        public void Configure(EntityTypeBuilder<QuizOption> builder)
        {
            builder.ToTable("QuizOptions");
            builder.HasKey(e => e.OptionId);
            builder.Property(e => e.OptionText).IsRequired();
            builder.HasOne(e => e.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
