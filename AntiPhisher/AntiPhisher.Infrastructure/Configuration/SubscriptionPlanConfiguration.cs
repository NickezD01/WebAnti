using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            // CHANGED: Name từ int(enum) → nvarchar(200) linh hoạt
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            // NEW: MaxSlots mặc định 10 nếu không truyền
            builder.Property(x => x.MaxSlots)
                .HasDefaultValue(10);
        }
    }
}
