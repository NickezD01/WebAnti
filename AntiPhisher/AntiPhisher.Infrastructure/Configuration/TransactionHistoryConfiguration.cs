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
    public class TransactionHistoryConfiguration : IEntityTypeConfiguration<TransactionHistory>
    {
        public void Configure(EntityTypeBuilder<TransactionHistory> builder)
        {
            //Transaction-Payment relationship
            builder.HasMany(p => p.Payments)
                 .WithOne(p => p.TransactionHistory)
                 .HasForeignKey(p => p.TransactionHId)
                 .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
