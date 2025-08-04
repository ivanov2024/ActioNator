using System;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Workout
{
    /// <summary>
    /// View model for exercises within a workout
    /// </summary>
    public class ExerciseViewModel
    {
        /// <summary>
        /// Unique identifier for the exercise
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the workout this exercise belongs to
        /// </summary>
        public Guid WorkoutId { get; set; }

        /// <summary>
        /// Name of the exercise
        /// </summary>
        [Required(ErrorMessage = "Exercise name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        [Display(Name = "Exercise Name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Number of sets for this exercise
        /// </summary>
        [Required(ErrorMessage = "Sets are required")]
        [Range(1, 100, ErrorMessage = "Sets must be between 1 and 100")]
        [Display(Name = "Sets")]
        public int Sets { get; set; }

        /// <summary>
        /// Number of repetitions per set
        /// </summary>
        [Required(ErrorMessage = "Reps are required")]
        [Range(1, 1000, ErrorMessage = "Reps must be between 1 and 1000")]
        [Display(Name = "Repetitions")]
        public int Reps { get; set; }

        /// <summary>
        /// Weight used for this exercise (in user's preferred unit)
        /// </summary>
        [Range(0, 1000, ErrorMessage = "Weight must be between 0 and 1000")]
        [Display(Name = "Weight")]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        public decimal Weight { get; set; }

        /// <summary>
        /// Additional notes about the exercise
        /// </summary>
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Duration of the exercise in minutes
        /// </summary>
        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 300, ErrorMessage = "Duration must be between 1 and 300 minutes")]
        [Display(Name = "Duration (minutes)")]
        public int Duration { get; set; }

        /// <summary>
        /// Reference to the exercise template this exercise is based on
        /// </summary>
        [Display(Name = "Exercise Template")]
        public Guid ExerciseTemplateId { get; set; }

        /// <summary>
        /// URL to the image representing this exercise
        /// </summary>
        [Url(ErrorMessage = "Please provide a valid URL")]
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Primary muscle group targeted by this exercise
        /// </summary>
        [Display(Name = "Targeted Muscle Group")]
        public string? TargetedMuscle { get; set; }
    }
}
