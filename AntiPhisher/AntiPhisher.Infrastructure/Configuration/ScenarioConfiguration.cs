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
    public class ScenarioConfiguration : IEntityTypeConfiguration<Scenario>
    {
        public void Configure(EntityTypeBuilder<Scenario> builder)
        {
            builder.HasKey(x => x.ScenarioId);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Subject)
                .HasMaxLength(500);

            builder.Property(x => x.SenderEmail)
                .HasMaxLength(255);

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Scenarios)
                .HasForeignKey(x => x.CategoryId);

            builder.HasOne(x => x.Difficulty)
                .WithMany(x => x.Scenarios)
                .HasForeignKey(x => x.DifficultyId);

            builder.HasMany(x => x.PhishingTypes)
                .WithMany(x => x.Scenarios)
                .UsingEntity<Dictionary<string, object>>(
                    "ScenarioPhishingTypes",
                    x => x.HasOne<PhishingType>()
                        .WithMany()
                        .HasForeignKey("PhishingTypeId"),
                    x => x.HasOne<Scenario>()
                        .WithMany()
                        .HasForeignKey("ScenarioId")
                );
        }
    }
}
