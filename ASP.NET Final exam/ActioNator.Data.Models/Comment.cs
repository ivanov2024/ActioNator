using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Comment;

namespace ActioNator.Data.Models
{
    public class Comment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AuthorId { get; set; }

        [Required]
        public virtual ApplicationUser Author { get; set; } = null!;

        public Guid? PostId { get; set; }

        public virtual Post? Post { get; set; }

        public Guid? JournalEntryId { get; set; }

        public virtual JournalEntry? JournalEntry { get; set; }

        [Required]
        [MinLength(ContentMinLength)]
        [MaxLength(ContentMaxLength)]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsEdited { get; set; }

        public bool IsDeleted { get; set; } 
    }
}
