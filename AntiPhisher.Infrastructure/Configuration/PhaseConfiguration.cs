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
    public class PhaseConfiguration : IEntityTypeConfiguration<Phase>
    {
        public void Configure(EntityTypeBuilder<Phase> builder)
        {
            builder.ToTable("Phases");

            builder.HasKey(e => e.PhaseId);

            builder.Property(e => e.PhaseName)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
