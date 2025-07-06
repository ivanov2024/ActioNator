using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Message;

namespace ActioNator.Data.Models
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public virtual ApplicationUser Sender { get; set; } = null!;

        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        public virtual ApplicationUser Receiver { get; set; } = null!;

        [Required]
        public Guid ChatId { get; set; }

        [Required]
        public virtual Chat Chat { get; set; } = null!;

        [Required]
        [MinLength(ContentMinLength)]
        [MaxLength(ContentMaxLength)]
        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }
    }
}
