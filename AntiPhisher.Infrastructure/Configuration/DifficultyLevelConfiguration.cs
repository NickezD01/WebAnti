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
    public class DifficultyLevelConfiguration
        : IEntityTypeConfiguration<DifficultyLevel>
    {
        public void Configure(EntityTypeBuilder<DifficultyLevel> builder)
        {
            builder.ToTable("DifficultyLevels");

            // PRIMARY KEY
            builder.HasKey(x => x.DifficultyId);

            builder.Property(x => x.DifficultyId)
                   .ValueGeneratedOnAdd();

            builder.Property(x => x.LevelName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.LevelOrder)
                   .IsRequired();

            builder.Property(x => x.BaseScore)
                   .IsRequired();

            builder.HasIndex(x => x.LevelName)
                   .IsUnique();

            // RELATIONSHIP
            builder.HasMany(x => x.Scenarios)
                   .WithOne(x => x.Difficulty)
                   .HasForeignKey(x => x.DifficultyId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
