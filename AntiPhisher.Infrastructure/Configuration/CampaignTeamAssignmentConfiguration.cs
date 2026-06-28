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
    public class CampaignTeamAssignmentConfiguration : IEntityTypeConfiguration<CampaignTeamAssignment>
    {
        public void Configure(EntityTypeBuilder<CampaignTeamAssignment> builder)
        {
            builder.HasKey(x => x.AssignmentId);

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignTeamAssignments)
                .HasForeignKey(x => x.CampaignId);

            builder.HasOne(x => x.Team)
                .WithMany(x => x.CampaignTeamAssignments)
                .HasForeignKey(x => x.TeamId);

            builder.HasOne(x => x.AssignedByNavigation)
                .WithMany(x => x.CampaignTeamAssignments)
                .HasForeignKey(x => x.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
