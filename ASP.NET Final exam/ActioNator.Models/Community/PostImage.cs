using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ActioNator.Models.Community
{
    public class PostImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Url { get; set; }

        public string Alt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public int PostId { get; set; }

        [ForeignKey(nameof(PostId))]
        public Post Post { get; set; }

        // TODO: Image upload logic goes here
        // Additional properties for image storage, dimensions, etc.
    }
}
