using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Exercise;

namespace ActioNator.Data.Models
{
    public class Exercise
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid WorkoutId { get; set; }

        [Required]
        public virtual Workout Workout { get; set; } = null!;

        [Required]
        public Guid ExerciseTemplateId { get; set; }

        [Required]
        public ExerciseTemplate ExerciseTemplate { get; set; } = null!;

        [Range(MinSets, MaxSets)]
        public int Sets { get; set; }

        [Range(MinReps, MaxReps)]
        public int Reps { get; set; }

        [Range(MinWeight, MaxWeight)]
        public decimal Weight { get; set; }

        [MinLength(NotesMinLength)]
        [MaxLength(NotesMaxLength)]
        public string? Notes { get; set; }

        [Required]
        public TimeSpan Duration { get; set; }

        public bool IsDeleted { get; set; }
    }
}
