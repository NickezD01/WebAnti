using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class UserFlawSummaryConfiguration : IEntityTypeConfiguration<UserFlawSummary>
    {
        public void Configure(EntityTypeBuilder<UserFlawSummary> builder)
        {
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.CurrentRiskScore)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0);

            builder.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 1-1 với User: UserId vừa là PK vừa là FK
            builder.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<UserFlawSummary>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}