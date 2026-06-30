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
    public class CampaignScenarioConfiguration : IEntityTypeConfiguration<CampaignScenario>
    {
        public void Configure(EntityTypeBuilder<CampaignScenario> builder)
        {
            builder.HasKey(x => x.CampaignScenarioId);

            builder.HasIndex(x => new
            {
                x.CampaignId,
                x.ScenarioId
            }).IsUnique();

            builder.HasOne(x => x.Campaign)
                .WithMany(x => x.CampaignScenarios)
                .HasForeignKey(x => x.CampaignId);

            builder.HasOne(x => x.Scenario)
                .WithMany(x => x.CampaignScenarios)
                .HasForeignKey(x => x.ScenarioId);
        }
    }
}
