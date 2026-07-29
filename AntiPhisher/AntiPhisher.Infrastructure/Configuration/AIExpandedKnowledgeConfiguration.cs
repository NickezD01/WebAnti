using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class AIExpandedKnowledgeConfiguration : IEntityTypeConfiguration<AIExpandedKnowledge>
    {
        public void Configure(EntityTypeBuilder<AIExpandedKnowledge> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
            builder.Property(x => x.ContextDescription).IsRequired();
            builder.Property(x => x.SourceType).IsRequired().HasMaxLength(50);
            builder.Property(x => x.SourceUrl).HasMaxLength(500);
            builder.Property(x => x.Tags).HasMaxLength(300);
            builder.Property(x => x.IsActive).HasDefaultValue(true);
            builder.Property(x => x.CreatedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
            builder.Property(x => x.ModifiedDate).HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.Difficulty)
                .WithMany()
                .HasForeignKey(x => x.DifficultyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}