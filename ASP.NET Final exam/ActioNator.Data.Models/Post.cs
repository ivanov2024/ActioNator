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


        [MinLength(ContentMinLength)]
        [MaxLength(ContentMaxLength)]
        public string? Content { get; set; }

        public int LikesCount { get; set; }

        public int SharesCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? ImageUrl { get; set; } = null!;

        public bool IsPublic { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
            = new HashSet<Comment>();

        public virtual ICollection<PostImage> PostImages { get; set; }
            = new HashSet<PostImage>();

        public virtual ICollection<PostLike> Likes { get; set; }
            = new HashSet<PostLike>();
    }
}
