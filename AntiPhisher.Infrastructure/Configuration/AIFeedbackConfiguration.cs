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
    public class AIFeedbackConfiguration : IEntityTypeConfiguration<AIFeedback>
    {
        public void Configure(EntityTypeBuilder<AIFeedback> builder)
        {
            builder.HasKey(x => x.FeedbackId);

            builder.HasOne(x => x.Attempt)
                .WithMany(x => x.AIFeedbacks)
                .HasForeignKey(x => x.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
