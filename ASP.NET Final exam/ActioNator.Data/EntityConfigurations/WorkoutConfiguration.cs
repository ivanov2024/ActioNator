using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ActioNator.Data.Models;

namespace ActioNator.Data.EntityConfigurations
{
    internal class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
    {
        public void Configure(EntityTypeBuilder<Workout> workout)
        {
            workout
                    .HasKey(w => w.Id);

            workout
                .HasOne(w => w.ApplicationUser)
                .WithMany(au => au.Workouts)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            workout
                .Property(w => w.Date)
                .HasDefaultValueSql("CAST(GETDATE() AS DATE)");

            workout
                .Property(w => w.Notes)
                .IsRequired(false);

            workout
                .Property(w => w.IsDeleted)
                .HasDefaultValue(false);

            workout
                .HasQueryFilter(w => !w.IsDeleted);
        }
    }
}
