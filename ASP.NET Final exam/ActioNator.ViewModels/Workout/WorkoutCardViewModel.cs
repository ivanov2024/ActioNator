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
        //[Display(Name = "Duration")]
        //public string DurationDisplay => $"{(int)Duration.TotalMinutes} mins";

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
        public string? CompletedDateDisplay => CompletedAt?.ToString("d");

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }

        [Display(Name = "Created On")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Please enter a valid date")]
        [CustomValidation(typeof(WorkoutCardViewModel), nameof(ValidateDate))]
        public DateTime? Date { get; set; }

        [Display(Name = "Created On")]
        [DataType(DataType.Date)]
        public string? DateDisplay 
            => Date?.ToString("d");

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
        public IEnumerable<ExerciseViewModel> Exercises 
        { 
            get => _exercises; 
            set => _exercises = value as ICollection<ExerciseViewModel> ?? new HashSet<ExerciseViewModel>(value); 
        }

        /// <summary>
        /// Custom validator to ensure a user-provided date is present and valid.
        /// Enforces: no missing, unparseable, or default(DateTime) values.
        /// </summary>
        /// <param name="value">The Date property value (boxed)</param>
        /// <param name="context">Validation context</param>
        /// <returns>ValidationResult indicating success or error message</returns>
        public static ValidationResult? ValidateDate(object value, ValidationContext context)
        {
            if (value == null)
            {
                return new ValidationResult("Please enter a valid date", new[] { nameof(Date) });
            }

            if (value is DateTime dt)
            {
                if (dt == default(DateTime))
                {
                    return new ValidationResult("Please enter a valid date", new[] { nameof(Date) });
                }
                return ValidationResult.Success;
            }

            // Any non-DateTime type is invalid for this property
            return new ValidationResult("Please enter a valid date", new[] { nameof(Date) });
        }
    }
}
