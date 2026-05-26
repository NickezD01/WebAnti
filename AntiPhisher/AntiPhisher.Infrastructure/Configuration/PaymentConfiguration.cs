using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");

            // Mối quan hệ giữa Payment và Order
            builder.HasOne(x => x.Order)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.OrderId);

            // ======================================================
            // FIX DỨT ĐIỂM: Khớp nối với UserConfiguration
            // ======================================================
            builder.HasOne(x => x.Account)                 // Sử dụng 'Account' khớp với cấu hình ở UserConfiguration (x.Account)
                .WithMany()                                // Để trống nếu trong class User không cần danh sách Payments, hoặc điền .WithMany(u => u.Payments) nếu có
                .HasForeignKey(x => x.AccountId)           // Khóa ngoại AccountId khớp hoàn toàn với bảng User
                .OnDelete(DeleteBehavior.Restrict);        // Ngắt cascade trực tiếp, chuyển sang Restrict để phá vỡ vòng lặp
        }
    }
}