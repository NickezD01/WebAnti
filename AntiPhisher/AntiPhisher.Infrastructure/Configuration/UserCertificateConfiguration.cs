using AntiPhisher.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AntiPhisher.Infrastructure.Configuration
{
    public class UserCertificateConfiguration : IEntityTypeConfiguration<UserCertificate>
    {
        public void Configure(EntityTypeBuilder<UserCertificate> builder)
        {
            builder.HasKey(x => x.CertificateId);

            builder.Property(x => x.CertificateCode)
                .IsRequired()
                .HasMaxLength(32);

            builder.HasIndex(x => x.CertificateCode)
                .IsUnique();

            builder.Property(x => x.CorrectRateSnapshot)
                .HasColumnType("decimal(5,2)");

            builder.Property(x => x.FullNameSnapshot)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.CompanyNameSnapshot)
                .HasMaxLength(256);

            builder.Property(x => x.IssuedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
