using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> post)
        {
            post
                .HasKey(p => p.Id);

            post
                .HasOne(p => p.ApplicationUser)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            post
                .Property(p => p.Content)
                .IsRequired(false);

            post
                .Property(p => p.CreatedAt)
                .HasDefaultValue(DateTime.UtcNow);

            post
                .Property(p => p.ImageUrl)
                .IsRequired(false);

            post
                .Property(p => p.IsPublic)
                .HasDefaultValue(false);

            post
                .Property(p => p.IsDeleted)
                .HasDefaultValue(false);

            post
                .HasQueryFilter(p => p.IsPublic && !p.IsDeleted);
        }
    }
}
