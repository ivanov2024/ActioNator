using System.ComponentModel.DataAnnotations;

namespace ActioNator.Data.Models
{
    public class UserLoginHistory
    {
        [Key]
        public Guid Id { get; set; }

        public DateTime LoginDate { get; set; }

        public Guid? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }
    }
}
