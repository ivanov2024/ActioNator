using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    public class PostLikeConfiguration : IEntityTypeConfiguration<PostLike>
    {
        public void Configure(EntityTypeBuilder<PostLike> postLike)
        {
            postLike.HasKey(pl => pl.Id);

            postLike.Property(pl => pl.CreatedAt)
                .IsRequired();

            postLike.Property(pl => pl.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Configure relationships
            postLike.HasOne(pl => pl.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            postLike.HasOne(pl => pl.ApplicationUser)
                .WithMany()
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Create a unique index on PostId and UserId to prevent duplicate likes
            postLike.HasIndex(pl => new { pl.PostId, pl.UserId })
                .IsUnique();
                
            // Add matching global query filter to match ApplicationUser entity's filter
            // This ensures PostLikes are filtered out when their associated User is filtered out
            postLike.HasQueryFilter(pl => !pl.ApplicationUser.IsDeleted);
        }
    }
}
