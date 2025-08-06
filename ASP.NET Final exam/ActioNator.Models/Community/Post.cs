using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ActioNator.Models.Community
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        [Required]
        public string AuthorId { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public ApplicationUser Author { get; set; }

        public int LikesCount { get; set; } = 0;

        public int CommentsCount { get; set; } = 0;

        public int SharesCount { get; set; } = 0;

        public bool IsCertified { get; set; } = false;

        public virtual ICollection<PostImage> Images { get; set; } = new List<PostImage>();

        public virtual ICollection<PostComment> Comments { get; set; } = new List<PostComment>();

        public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    }
}
