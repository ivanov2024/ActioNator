using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    public class CommentLikeConfiguration : IEntityTypeConfiguration<CommentLike>
    {
        public void Configure(EntityTypeBuilder<CommentLike> commentLike)
        {
            commentLike.HasKey(cl => cl.Id);

            commentLike.Property(cl => cl.CreatedAt)
                .IsRequired();

            commentLike.Property(cl => cl.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Configure relationships
            commentLike.HasOne(cl => cl.Comment)
                .WithMany(c => c.Likes)
                .HasForeignKey(cl => cl.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            commentLike.HasOne(cl => cl.ApplicationUser)
                .WithMany()
                .HasForeignKey(cl => cl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create a unique index on CommentId and UserId to prevent duplicate likes
            commentLike.HasIndex(cl => new { cl.CommentId, cl.UserId })
                .IsUnique();
                
            // Add matching global query filters to match both Comment and ApplicationUser entity filters
            // This ensures CommentLikes are filtered out when their associated Comment or User is filtered out
            commentLike.HasQueryFilter(cl => !cl.Comment.IsDeleted && !cl.ApplicationUser.IsDeleted);
        }
    }
}
