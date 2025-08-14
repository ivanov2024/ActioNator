using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    public class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
    {
        public void Configure(EntityTypeBuilder<UserReport> userReport)
        {
            userReport.HasKey(ur => ur.Id);

            userReport.Property(ur => ur.Reason)
                .IsRequired()
                .HasMaxLength(100);

            userReport.Property(ur => ur.Details)
                .HasMaxLength(1000);

            userReport.Property(ur => ur.CreatedAt)
                .IsRequired();

            userReport.Property(ur => ur.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            userReport.Property(ur => ur.ReviewNotes)
                .HasMaxLength(1000);

            // Relationships
            userReport.HasOne(ur => ur.ReportedUser)
                .WithMany()
                .HasForeignKey(ur => ur.ReportedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            userReport.HasOne(ur => ur.ReportedByUser)
                .WithMany()
                .HasForeignKey(ur => ur.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            userReport.HasOne(ur => ur.ReviewedByUser)
                .WithMany()
                .HasForeignKey(ur => ur.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            userReport.HasQueryFilter(ur => !ur.ReportedUser.IsDeleted);
        }
    }
}
