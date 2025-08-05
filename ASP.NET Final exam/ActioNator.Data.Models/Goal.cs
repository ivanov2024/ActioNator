using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Goal;

namespace ActioNator.Data.Models
{
    public class Goal
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ApplicationUserId { get; set; }

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

        public DateTime CreatedAt { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedAt { get; set; } = null!;

        public bool IsDeleted { get; set; }
    }
}
