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
    public class TeamConfiguration : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.HasKey(x => x.TeamId);

            builder.Property(x => x.TeamName)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasOne(x => x.Company)
                .WithMany(x => x.Teams)
                .HasForeignKey(x => x.CompanyId);

            builder.HasOne(x => x.Manager)
                .WithMany(x => x.Teams)
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
