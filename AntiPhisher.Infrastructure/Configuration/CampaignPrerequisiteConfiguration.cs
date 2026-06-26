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
    public class CampaignPrerequisiteConfiguration : IEntityTypeConfiguration<CampaignPrerequisite>
    {
        public void Configure(EntityTypeBuilder<CampaignPrerequisite> builder)
        {
            builder.ToTable("CampaignPrerequisites");

            builder.HasKey(e => e.Id);

            builder.HasOne(d => d.Campaign)
                .WithMany(c => c.CampaignPrerequisites)
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.RequiredLesson)
                .WithMany(l => l.CampaignPrerequisites)
                .HasForeignKey(d => d.RequiredLessonId)
                .OnDelete(DeleteBehavior.ClientSetNull); // Tránh bẫy Multiple Cascade Paths trong SQL
        }
    }
}
