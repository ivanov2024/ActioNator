using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class GoalConfiguration : IEntityTypeConfiguration<Goal>
    {
        public void Configure(EntityTypeBuilder<Goal> goal)
        {
            goal
                .HasKey(g => g.Id);

            goal
                .HasOne(g => g.ApplicationUser)
                .WithMany(au => au.Goals)
                .HasForeignKey(g => g.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            goal
                .Property(g => g.CreatedAt)
                .HasDefaultValue(DateTime.UtcNow);

            goal
                .Property(g => g.DueDate)
                .IsRequired(false);

            goal
                .Property(g => g.IsCompleted)
                .HasDefaultValue(false);

            goal
                .Property(g => g.CompletedAt)
                .IsRequired(false);

            goal
                .Property(g => g.IsDeleted)
                .HasDefaultValue(false);

            goal
                .HasQueryFilter(g => g.IsDeleted == false);
        }
    }
}
