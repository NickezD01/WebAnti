using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    // Cấu hình RIÊNG cho các field mới thêm ở Scenario.Review.cs.
    // Không đụng tới ScenarioConfiguration.cs gốc (nếu có) — EF Core cho phép
    // nhiều IEntityTypeConfiguration<T> cùng config 1 entity, miễn không đè lẫn nhau.
    public class ScenarioReviewConfiguration : IEntityTypeConfiguration<Scenario>
    {
        public void Configure(EntityTypeBuilder<Scenario> builder)
        {
            builder.Property(x => x.GenerationStatus)
                .HasMaxLength(20)
                .HasDefaultValue("Manual");

            builder.Property(x => x.SourceScenarioIds)
                .HasMaxLength(200);

            builder.Property(x => x.ContentHash)
                .HasMaxLength(32);

            // FK thêm tới User (ReviewedByUserId) — dùng OnDelete Restrict
            // vì Scenario đã có FK CreatedByUserId tới User, SQL Server không cho phép
            // 2 đường cascade-delete cùng trỏ về 1 bảng từ 1 bảng con.
            builder.HasOne(x => x.ReviewedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}