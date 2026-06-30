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
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.AvatarUrl)
                .HasMaxLength(500);

            builder.HasIndex(x => x.Email)
                .IsUnique();

            // User -> Role
            builder.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Company
            builder.HasOne(x => x.Company)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            // User -> Campaign (CreatedBy)
            builder.HasMany(x => x.Campaigns)
                .WithOne(x => x.CreatedByUser)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Scenario (CreatedBy)
            builder.HasMany(x => x.Scenarios)
                .WithOne(x => x.CreatedByUser)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // User -> Team (Manager)
            builder.HasMany(x => x.Teams)
                .WithOne(x => x.Manager)
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> UserAttempts
            builder.HasMany(x => x.UserAttempts)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Notifications
            builder.HasMany(x => x.Notifications)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> LoginHistory
            builder.HasMany(x => x.LoginHistories)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> RefreshTokens
            builder.HasMany(x => x.RefreshTokens)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> TeamMembers
            builder.HasMany(x => x.TeamMembers)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> CampaignUserAssignments (AssignedBy)
            builder.HasMany(x => x.CampaignUserAssignmentAssignedByNavigations)
                .WithOne(x => x.AssignedByNavigation)
                .HasForeignKey(x => x.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> CampaignUserAssignments (User)
            builder.HasMany(x => x.CampaignUserAssignmentUsers)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> CampaignTeamAssignments
            builder.HasMany(x => x.CampaignTeamAssignments)
                .WithOne(x => x.AssignedByNavigation)
                .HasForeignKey(x => x.AssignedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Subscriptions
            builder.HasMany(x => x.Subscriptions)
                .WithOne(x => x.Account)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> EmailVerifications
            builder.HasMany(x => x.EmailVerifications)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
