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
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.UserAccount)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Payments)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId);
        }
    }
}
