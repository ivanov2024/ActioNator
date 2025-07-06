using System.ComponentModel.DataAnnotations;

namespace ActioNator.Data.Models
{
    public class Chat
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid Participant1Id { get; set; }

        [Required]
        public virtual ApplicationUser Participant1 { get; set; } = null!;

        [Required]
        public Guid Participant2Id { get; set; }

        [Required]
        public virtual ApplicationUser Participant2 { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime LastMessageAt { get; set; }

        public virtual ICollection<Message> Messages { get; set; } 
         = new HashSet<Message>();
    }
}
