using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    public class PostReportConfiguration : IEntityTypeConfiguration<PostReport>
    {
        public void Configure(EntityTypeBuilder<PostReport> postReport)
        {
            postReport.HasKey(pr => pr.Id);

            postReport.Property(pr => pr.Reason)
                .IsRequired()
                .HasMaxLength(100);

            postReport.Property(pr => pr.Details)
                .HasMaxLength(1000);

            postReport.Property(pr => pr.CreatedAt)
                .IsRequired();

            postReport.Property(pr => pr.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            postReport.Property(pr => pr.ReviewNotes)
                .HasMaxLength(1000);

            // Configure relationships
            postReport.HasOne(pr => pr.Post)
                .WithMany()
                .HasForeignKey(pr => pr.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            postReport.HasOne(pr => pr.ReportedByUser)
                .WithMany()
                .HasForeignKey(pr => pr.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            postReport.HasOne(pr => pr.ReviewedByUser)
                .WithMany()
                .HasForeignKey(pr => pr.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Add matching global query filter to match Post entity's filter
            // This ensures PostReports are filtered out when their associated Post is filtered out
            postReport.HasQueryFilter(pr => pr.Post.IsPublic && !pr.Post.IsDeleted);
        }
    }
}
