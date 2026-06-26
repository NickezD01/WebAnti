using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class UserLessonProgressConfiguration : IEntityTypeConfiguration<UserLessonProgress>
    {
        public void Configure(EntityTypeBuilder<UserLessonProgress> builder)
        {
            builder.ToTable("UserLessonProgresses");

            builder.HasKey(e => e.ProgressId);

            // Đảm bảo mỗi User chỉ có độc nhất 1 bản ghi tiến độ đối với mỗi bài học
            builder.HasIndex(e => new { e.UserId, e.LessonId })
                .IsUnique();

            builder.HasOne(d => d.Lesson)
                .WithMany(l => l.UserLessonProgresses)
                .HasForeignKey(d => d.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
