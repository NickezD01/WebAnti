using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            // NEW: UsedSlots mặc định 0
            builder.Property(x => x.UsedSlots)
                .HasDefaultValue(0);

            // Quan hệ Account (Manager) → Subscriptions
            builder.HasOne(x => x.Account)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ SubscriptionPlan
            builder.HasOne(x => x.SubscriptionPlans)
                .WithMany(x => x.Subscriptions)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // NEW: Quan hệ Company → Subscriptions (nullable, không xóa cascade)
            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Quan hệ Order (1-1)
            builder.HasOne(x => x.Order)
                .WithOne(x => x.Subscription)
                .HasForeignKey<Order>(x => x.SubscriptionId);
        }
    }
}
