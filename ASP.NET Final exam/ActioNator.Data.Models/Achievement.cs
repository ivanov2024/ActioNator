using System.ComponentModel.DataAnnotations;

namespace ActioNator.Data.Models
{
    public class Achievement
    {
        [Key]
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        public virtual ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public Guid AchievementTemplateId { get; set; }

        [Required]
        public AchievementTemplate AchievementTemplate { get; set; } = null!;

        public DateTime? AchievedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
