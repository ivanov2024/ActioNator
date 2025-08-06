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
                    Id = new Guid("bb7a6489-bc67-4caf-b0d2-4bc334313028"),
                    Name = "Push Up",
                    ImageUrl = "~/images/exercise/pushUpImage.png",
                    TargetedMuscle = "Chest"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("8041d570-86bb-4587-8ae9-40633155caa5"),
                    Name = "Pull Up",
                    ImageUrl = "~/images/exercise/pullUpImage.png",
                    TargetedMuscle = "Back"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("5bf35923-36de-4e77-b3b8-e6b84300c8ec"),
                    Name = "Squat",
                    ImageUrl = "~/images/exercise/squatImage.webp",
                    TargetedMuscle = "Legs"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("7eed864d-10f3-4dfe-9966-4c796f30dddd"),
                    Name = "Deadlift",
                    ImageUrl = "~/images/exercise/deadliftImage.png",
                    TargetedMuscle = "Back"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("d3a16e73-7d1b-419e-8d32-b7f639eb33f1"),
                    Name = "Plank",
                    ImageUrl = "~/images/exercise/plankImage.png",
                    TargetedMuscle = "Core"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("710375cf-8cda-4667-8ee5-1461be8e069f"),
                    Name = "Bicep Curl",
                    ImageUrl = "~/images/exercise/bicepCurlImage.webp",
                    TargetedMuscle = "Biceps"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("971a0449-a9dd-4ad5-8982-c1a9cf9224be"),
                    Name = "Tricep Dip",
                    ImageUrl = "~/images/exercise/tricepDipImage.png",
                    TargetedMuscle = "Triceps"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("4004adad-d275-492b-89b3-ffabc268f148"),
                    Name = "Shoulder Press",
                    ImageUrl = "~/images/exercise/shoulderPressImage.webp",
                    TargetedMuscle = "Shoulders"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("f4de6e2a-a6d3-435b-bcb6-61f6fc15ef39"),
                    Name = "Lunges",
                    ImageUrl = "~/images/exercise/lungesImage.webp",
                    TargetedMuscle = "Legs"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("9aacb5df-d1d9-4b7d-8008-7b3d66bd8f23"),
                    Name = "Mountain Climbers",
                    ImageUrl = "~/images/exercise/mountainClimbersImage.png",
                    TargetedMuscle = "Full Body"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("c0c4b16c-10be-4149-b409-dca4939e4ff0"),
                    Name = "Burpees",
                    ImageUrl = "~/images/exercise/burpeesImage.webp",
                    TargetedMuscle = "Full Body"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("3c5d42b2-326c-44df-b69d-73088a57df74"),
                    Name = "Russian Twists",
                    ImageUrl = "~/images/exercise/russianTwistsImage.webp",
                    TargetedMuscle = "Obliques"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("4870de5c-1b12-435b-b0f9-1329b79dfc11"),
                    Name = "Jumping Jacks",
                    ImageUrl = "~/images/exercise/jumpingJacksImage.png",
                    TargetedMuscle = "Cardio"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("295ac6a3-afd1-4ae3-b3f5-96fb333fba3a"),
                    Name = "High Knees",
                    ImageUrl = "~/images/exercise/highKneesImage.webp",
                    TargetedMuscle = "Cardio"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("1968868c-7b65-4a46-a750-74c658238871"),
                    Name = "Leg Raises",
                    ImageUrl = "~/images/exercise/legRaisesImage.webp",
                    TargetedMuscle = "Lower Abs"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("59bc6663-b166-4f93-89bb-63b6a5d4ce31"),
                    Name = "Glute Bridge",
                    ImageUrl = "~/images/exercise/gluteBridgeImage.webp",
                    TargetedMuscle = "Glutes"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("d55f03b7-eaa5-4954-a8df-9149d2a224cf"),
                    Name = "Side Plank",
                    ImageUrl = "~/images/exercise/sidePlankImage.png",
                    TargetedMuscle = "Obliques"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("872da8e9-d5bb-41f0-bfbe-3af6d3d67610"),
                    Name = "Wall Sit",
                    ImageUrl = "~/images/exercise/wallSitImage.png",
                    TargetedMuscle = "Quadriceps"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("5ab47cce-b62f-4fee-be3e-56c6ea52f0e9"),
                    Name = "Calf Raise",
                    ImageUrl = "~/images/exercise/calfRaiseImage.jpeg",
                    TargetedMuscle = "Calves"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("9987cc3b-0b90-497c-bbce-ef76ea8938e8"),
                    Name = "Bent Over Row",
                    ImageUrl = "~/images/exercise/bentOverRowImage.webp",
                    TargetedMuscle = "Upper Back"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("367b75c9-fc39-41b5-b4c4-e6d5575866d1"),
                    Name = "Chest Fly",
                    ImageUrl = "~/images/exercise/chestFlyImage.webp",
                    TargetedMuscle = "Chest"
                },
                new ExerciseTemplate
                {
                    Id = new Guid("01c64ba0-e196-4440-93aa-4a625b342122"),
                    Name = "Toe Touches",
                    ImageUrl = "~/images/exercise/toeTouchesImage.webp",
                    TargetedMuscle = "Hamstrings"
                }
                );
        }
    }
}
