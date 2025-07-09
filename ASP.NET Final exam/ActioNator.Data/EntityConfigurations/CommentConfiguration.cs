using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> comment)
        {
            comment
                .HasKey(c => c.Id);

            comment
                .HasOne(c => c.Author)
                .WithMany(a => a.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            comment
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            comment
                .HasOne(c => c.JournalEntry)
                .WithMany(je => je.Comments)
                .HasForeignKey(c => c.JournalEntryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            comment
                .Property(c => c.CreatedAt)
                .HasDefaultValue(DateTime.UtcNow);

            comment
                .Property(c => c.IsEdited)
                .HasDefaultValue(false);

            comment
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            comment
                .HasQueryFilter(c => c.IsDeleted == false);
        }
    }
}
