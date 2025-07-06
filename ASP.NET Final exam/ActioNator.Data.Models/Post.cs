using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Post;

namespace ActioNator.Data.Models
{
    public class Post
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

        [MinLength(ContentMinLength)]
        [MaxLength(ContentMaxLength)]
        public string? Content { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ImageUrl { get; set; } = null!;

        public bool IsPublic { get; set; }
    }
}
