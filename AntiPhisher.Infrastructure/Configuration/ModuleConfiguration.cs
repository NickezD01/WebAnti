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
    public class ModuleConfiguration : IEntityTypeConfiguration<Module>
    {
        public void Configure(EntityTypeBuilder<Module> builder)
        {
            builder.ToTable("Modules");

            builder.HasKey(e => e.ModuleId);

            builder.Property(e => e.ModuleName)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(d => d.Phase)
                .WithMany(p => p.Modules)
                .HasForeignKey(d => d.PhaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
