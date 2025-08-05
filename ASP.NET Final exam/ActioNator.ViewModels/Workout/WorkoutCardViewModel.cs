using ActioNator.ViewModels.Workout;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Workouts
{
    /// <summary>
    /// View model for workout cards displayed in the UI
    /// </summary>
    public class WorkoutCardViewModel
    {
        private TimeSpan _duration;
        private ICollection<ExerciseViewModel> _exercises;
        
        /// <summary>
        /// Initializes a new instance of the WorkoutCardViewModel class
        /// </summary>
        public WorkoutCardViewModel()
        {
            // Initialize with default values
            _duration = TimeSpan.Zero;
            _exercises = new HashSet<ExerciseViewModel>();
        }
        
        /// <summary>
        /// Unique identifier for the workout
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Title of the workout
        /// </summary>
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 100 characters")]
        [Display(Name = "Workout Title")]
        public string Title { get; set; } = null!;

        /// <summary>
        /// Duration of the workout (calculated server-side from exercises)
        /// </summary>
        [Display(Name = "Duration")]
        public TimeSpan Duration 
        { 
            get => _duration; 
            set => _duration = value; 
        }
        
        /// <summary>
        /// Formatted display of the workout duration
        /// </summary>
        [Display(Name = "Duration")]
        public string DurationDisplay => $"{(int)Duration.TotalMinutes} mins";

        /// <summary>
        /// Date and time when the workout was completed
        /// </summary>
        [Display(Name = "Completed On")]
        [DataType(DataType.DateTime)]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Formatted display of the completion date
        /// </summary>
        [Display(Name = "Completed On")]
        [DataType(DataType.Date)]
        public string? CompletedDateDisplay => CompletedAt?.ToString("D");
        
        /// <summary>
        /// Additional notes about the workout
        /// </summary>
        [Display(Name = "Notes")]
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        /// <summary>
        /// Collection of exercises included in this workout
        /// </summary>
        [Display(Name = "Exercises")]
        [MinLength(1, ErrorMessage = "At least one exercise is required")]
        public IEnumerable<ExerciseViewModel> Exercises 
        { 
            get => _exercises; 
            set => _exercises = value as ICollection<ExerciseViewModel> ?? new HashSet<ExerciseViewModel>(value); 
        }
    }
}
