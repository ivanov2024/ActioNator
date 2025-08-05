using System.ComponentModel.DataAnnotations;

namespace ActioNator.Data.Models
{
    public class PostImage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string ImageUrl { get; set; } = null!;

        public Guid? PostId { get; set; }

        public virtual Post? Post { get; set; }
    }
}
