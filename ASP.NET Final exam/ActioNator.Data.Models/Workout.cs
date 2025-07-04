using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Workout;

namespace ActioNator.Data.Models
{
    public class Workout
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        [Required]
        public DateTime Date { get; set; }

        [MinLength(TitleMinLength)]
        [MaxLength(TitleMaxLength)]
        public string Title { get; set; } = null!;

        [MinLength(NotesMinLength)]
        [MaxLength(NotesMaxLength)]
        public string? Notes { get; set; }

        [Required]
        public TimeSpan Duration { get; set; }

        public virtual ICollection<Exercise> Exercises { get; set; } 
            = new HashSet<Exercise>();
    }

}
