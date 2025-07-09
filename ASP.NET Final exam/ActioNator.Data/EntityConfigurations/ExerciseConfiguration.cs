using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
    {
        public void Configure(EntityTypeBuilder<Exercise> exercise)
        {
            exercise
                .HasKey(e => e.Id);

            exercise
                .HasOne(e => e.Workout)
                .WithMany(w => w.Exercises)
                .HasForeignKey(e => e.WorkoutId)
                .OnDelete(DeleteBehavior.Restrict);

            exercise
                .HasOne(e => e.ExerciseTemplate)
                .WithMany(et => et.Exercises)
                .HasForeignKey(e => e.ExerciseTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            exercise
                .Property(e => e.Weight)
                .HasColumnType("decimal(18,2)");

            exercise
                .Property(e => e.Notes)
                .IsRequired(false);

            exercise
                .Property(e => e.IsDeleted)
                .HasDefaultValue(false);

            exercise
                .HasQueryFilter(e => e.IsDeleted == false);
        }
    }
}
