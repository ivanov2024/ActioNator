using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Achievement;

namespace ActioNator.Data.Models
{
    public class Achievement
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        [Required]
        [MinLength(TitleMinLength)]
        [MaxLength(TitleMaxLength)]
        public string Title { get; set; } = null!;

        [Required]
        [MinLength(DescriptionMinLength)]
        [MaxLength(DescriptionMaxLength)]
        public string Description { get; set; } = null!;

        public DateTime? AchievedAt { get; set; }

        [Required]
        public string ImageUrl { get; set; } = null!;

        public bool IsActive { get; set; }
    }
}
