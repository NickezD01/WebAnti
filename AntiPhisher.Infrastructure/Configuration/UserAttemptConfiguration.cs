using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class UserAttemptConfiguration : IEntityTypeConfiguration<UserAttempt>
    {
        public void Configure(EntityTypeBuilder<UserAttempt> builder)
        {
            builder.HasKey(x => x.AttemptId);

            // CHANGED: UserAnswer nullable, tối đa 50 ký tự (Reported/Clicked/NoAction)
            builder.Property(x => x.UserAnswer)
                .HasMaxLength(50)
                .IsRequired(false);

            // NEW: 3 trường hành vi phishing, mặc định false
            builder.Property(x => x.IsClickedLink).HasDefaultValue(false);
            builder.Property(x => x.IsCredentialLeaked).HasDefaultValue(false);
            builder.Property(x => x.IsReported).HasDefaultValue(false);

            builder.HasOne(x => x.User)
                .WithMany(x => x.UserAttempts)
                .HasForeignKey(x => x.UserId);

            builder.HasOne(x => x.Scenario)
                .WithMany(x => x.UserAttempts)
                .HasForeignKey(x => x.ScenarioId);

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.UserAttempts)
                .HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
