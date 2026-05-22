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
    public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
    {
        public void Configure(EntityTypeBuilder<TeamMember> builder)
        {
            builder.HasKey(x => x.TeamMemberId);

            builder.HasIndex(x => new
            {
                x.TeamId,
                x.UserId
            }).IsUnique();

            builder.HasOne(x => x.Team)
                .WithMany(x => x.TeamMembers)
                .HasForeignKey(x => x.TeamId);

            builder.HasOne(x => x.User)
                .WithMany(x => x.TeamMembers)
                .HasForeignKey(x => x.UserId);
        }
    }
}
