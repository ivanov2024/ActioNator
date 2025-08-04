using System;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Workout
{
    /// <summary>
    /// View model for exercise templates
    /// </summary>
    public class ExerciseTemplateViewModel
    {
        /// <summary>
        /// Unique identifier for the exercise template
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the exercise template
        /// </summary>
        [Required(ErrorMessage = "Exercise name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// URL to the image representing this exercise
        /// </summary>
        [Url(ErrorMessage = "Please provide a valid URL")]
        [StringLength(2000, ErrorMessage = "URL cannot exceed 2000 characters")]
        public string? ImageUrl { get; set; }
        
        /// <summary>
        /// Primary muscle group targeted by this exercise
        /// </summary>
        [StringLength(50, ErrorMessage = "Targeted muscle name cannot exceed 50 characters")]
        public string? TargetedMuscle { get; set; }
    }
}
