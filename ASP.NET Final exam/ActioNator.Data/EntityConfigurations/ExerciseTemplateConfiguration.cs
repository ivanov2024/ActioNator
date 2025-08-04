using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class ExerciseTemplateConfiguration : IEntityTypeConfiguration<ExerciseTemplate>
    {
        public void Configure(EntityTypeBuilder<ExerciseTemplate> exerciseTemplate)
        {
            exerciseTemplate
                .HasKey(et => et.Id);

            exerciseTemplate
                .HasData(
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Push Up",
                    ImageUrl = "~/images/exercise/pushUpImage.png",
                    TargetedMuscle = "Chest"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Pull Up",
                    ImageUrl = "~/images/exercise/pullUpImage.png",
                    TargetedMuscle = "Back"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Squat",
                    ImageUrl = "~/images/exercise/squatImage.webp",
                    TargetedMuscle = "Legs"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Deadlift",
                    ImageUrl = "~/images/exercise/deadliftImage.png",
                    TargetedMuscle = "Back"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Plank",
                    ImageUrl = "~/images/exercise/plankImage.png",
                    TargetedMuscle = "Core"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Bicep Curl",
                    ImageUrl = "~/images/exercise/bicepCurlImage.webp",
                    TargetedMuscle = "Biceps"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Tricep Dip",
                    ImageUrl = "~/images/exercise/tricepDipImage.png",
                    TargetedMuscle = "Triceps"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Shoulder Press",
                    ImageUrl = "~/images/exercise/shoulderPressImage.webp",
                    TargetedMuscle = "Shoulders"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Lunges",
                    ImageUrl = "~/images/exercise/lungesImage.webp",
                    TargetedMuscle = "Legs"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Mountain Climbers",
                    ImageUrl = "~/images/exercise/mountainClimbersImage.png",
                    TargetedMuscle = "Full Body"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Burpees",
                    ImageUrl = "~/images/exercise/burpeesImage.webp",
                    TargetedMuscle = "Full Body"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Russian Twists",
                    ImageUrl = "~/images/exercise/russianTwistsImage.webp",
                    TargetedMuscle = "Obliques"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Jumping Jacks",
                    ImageUrl = "~/images/exercise/jumpingJacksImage.png",
                    TargetedMuscle = "Cardio"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "High Knees",
                    ImageUrl = "~/images/exercise/highKneesImage.webp",
                    TargetedMuscle = "Cardio"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Leg Raises",
                    ImageUrl = "~/images/exercise/legRaisesImage.webp",
                    TargetedMuscle = "Lower Abs"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Glute Bridge",
                    ImageUrl = "~/images/exercise/gluteBridgeImage.webp",
                    TargetedMuscle = "Glutes"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Side Plank",
                    ImageUrl = "~/images/exercise/sidePlankImage.png",
                    TargetedMuscle = "Obliques"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Wall Sit",
                    ImageUrl = "~/images/exercise/wallSitImage.png",
                    TargetedMuscle = "Quadriceps"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Calf Raise",
                    ImageUrl = "~/images/exercise/calfRaiseImage.jpeg",
                    TargetedMuscle = "Calves"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Bent Over Row",
                    ImageUrl = "~/images/exercise/bentOverRowImage.webp",
                    TargetedMuscle = "Upper Back"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Chest Fly",
                    ImageUrl = "~/images/exercise/chestFlyImage.webp",
                    TargetedMuscle = "Chest"
                },
                new ExerciseTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Toe Touches",
                    ImageUrl = "~/images/exercise/toeTouchesImage.webp",
                    TargetedMuscle = "Hamstrings"
                }
                );
        }
    }
}
