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
    public class CampaignUserAssignmentConfiguration : IEntityTypeConfiguration<CampaignUserAssignment>
    {
        public void Configure(EntityTypeBuilder<CampaignUserAssignment> builder)
        {
            builder.HasKey(x => x.AssignmentId);

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignUserAssignments)
                .HasForeignKey(x => x.CampaignId);

            builder.HasOne(x => x.User)
                .WithMany(x => x.CampaignUserAssignmentUsers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.AssignedByNavigation)
                .WithMany(x => x.CampaignUserAssignmentAssignedByNavigations)
                .HasForeignKey(x => x.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
