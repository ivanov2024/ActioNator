using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class ChatConfiguration : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> chat)
        {
            chat
                .HasKey(c => c.Id);

            chat
                .HasOne(c => c.Participant1)
                .WithMany(p => p.ChatsInitiated)
                .HasForeignKey(c => c.Participant1Id)
                .OnDelete(DeleteBehavior.Restrict);

            chat
                .HasOne(c => c.Participant2)
                .WithMany(p => p.ChatsReceived)
                .HasForeignKey(c => c.Participant2Id)
                .OnDelete(DeleteBehavior.Restrict);

            chat
                .Property(c => c.IsDeleted)
                .HasDefaultValue(false);

            chat
                .HasQueryFilter(c => c.IsDeleted == false);
        }
    }
}
