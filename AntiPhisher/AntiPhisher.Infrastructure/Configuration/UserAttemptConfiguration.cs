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
    public class UserAttemptConfiguration : IEntityTypeConfiguration<UserAttempt>
    {
        public void Configure(EntityTypeBuilder<UserAttempt> builder)
        {
            builder.HasKey(x => x.AttemptId);

            builder.Property(x => x.UserAnswer)
                .HasMaxLength(20);

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
