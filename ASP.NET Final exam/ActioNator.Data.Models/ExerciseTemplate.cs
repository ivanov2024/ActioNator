using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.ExerciseTemplate;

namespace ActioNator.Data.Models
{
    public class ExerciseTemplate
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MinLength(NameMinLength)]
        [MaxLength(NameMaxLength)]
        public string Name { get; set; } = null!;

        [Required]
        public string ImageUrl { get; set; } = null!;

        [Required]
        public string TargetedMuscle { get; set; } = null!;

        public ICollection<Exercise> Exercises { get; set; } = new HashSet<Exercise>();
    }
}
