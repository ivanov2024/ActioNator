using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> message)
        {
            message
                .HasKey(m => m.Id);

            message
                .HasOne(m => m.Receiver)
                .WithMany(au => au.MessagesReceived)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            message
                .HasOne(m => m.Sender)
                .WithMany(au => au.MessagesSent)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            message
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Restrict);

            message
                .Property(m => m.PostedAt)
                .HasDefaultValue(DateTime.UtcNow);

            message
                .Property(m => m.IsRead)
                .HasDefaultValue(false);

            message
                .Property(m => m.IsDeleted)
                .HasDefaultValue(false);

            message
                .HasQueryFilter(m => m.IsDeleted == false);
        }
    }
}
