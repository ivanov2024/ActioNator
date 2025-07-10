using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
    {
        public void Configure(EntityTypeBuilder<JournalEntry> journalEntry)
        {
            journalEntry
                .HasKey(je => je.Id);

            journalEntry
                .HasOne(je => je.ApplicationUser)
                .WithMany(au => au.JournalEntries)
                .HasForeignKey(je => je.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            journalEntry
                .Property(je => je.CreatedAt)
                .HasDefaultValue(DateTime.UtcNow);

            journalEntry
                .Property(je => je.MoodTag)
                .IsRequired(false);

            journalEntry
                .Property(je => je.ImageUrl)
                .IsRequired(false);

            journalEntry
                .Property(je => je.IsPublic)
                .HasDefaultValue(false);

            journalEntry
                .Property(je => je.IsDeleted)
                .HasDefaultValue(false);

            journalEntry
                .HasQueryFilter(je => je.IsDeleted == false);
        }
    }
}
