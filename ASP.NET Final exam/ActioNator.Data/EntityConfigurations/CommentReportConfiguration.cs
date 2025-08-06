using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    public class CommentReportConfiguration : IEntityTypeConfiguration<CommentReport>
    {
        public void Configure(EntityTypeBuilder<CommentReport> commentReport)
        {
            commentReport.HasKey(cr => cr.Id);

            commentReport.Property(cr => cr.Reason)
                .IsRequired()
                .HasMaxLength(100);

            commentReport.Property(cr => cr.Details)
                .HasMaxLength(1000);

            commentReport.Property(cr => cr.CreatedAt)
                .IsRequired();

            commentReport.Property(cr => cr.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            commentReport.Property(cr => cr.ReviewNotes)
                .HasMaxLength(1000);

            // Configure relationships
            commentReport.HasOne(cr => cr.Comment)
                .WithMany()
                .HasForeignKey(cr => cr.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            commentReport.HasOne(cr => cr.ReportedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            commentReport.HasOne(cr => cr.ReviewedByUser)
                .WithMany()
                .HasForeignKey(cr => cr.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Add matching global query filter to match Comment entity's filter
            // This ensures CommentReports are filtered out when their associated Comment is filtered out
            commentReport.HasQueryFilter(cr => !cr.Comment.IsDeleted);
        }
    }
}
