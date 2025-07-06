using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.JournalEntry;

namespace ActioNator.Data.Models
{
    public class JournalEntry
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public virtual ApplicationUser ApplicationUser { get; set; } = null!;

        [MinLength(TitleMinLength)]
        [MaxLength(TitleMaxLength)]
        public string Title { get; set; } = null!;

        [Required]
        [MinLength(ContentMinLength)]
        [MaxLength(ContentMaxLength)]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        [MinLength(MoodTagMinLength)]
        [MaxLength(MoodTagMaxLength)]
        public string? MoodTag { get; set; } = null!;

        public string? ImageUrl { get; set; } = null!;

        public bool IsPublic { get; set; }

        public bool IsDeleted { get; set; }
    }
}
