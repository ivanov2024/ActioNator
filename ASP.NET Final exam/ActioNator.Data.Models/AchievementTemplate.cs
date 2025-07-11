using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.AchievementTemplate;

namespace ActioNator.Data.Models
{
    public class AchievementTemplate
    {
        public Guid Id { get; set; }

        [Required]
        [MinLength(TitleMinLength)]
        [MaxLength(TitleMaxLength)]
        public string Title { get; set; } = null!;

        [Required]
        [MinLength(DescriptionMinLength)]
        [MaxLength(DescriptionMaxLength)]
        public string Description { get; set; } = null!;

        [Required]
        public string ImageUrl { get; set; } = null!;

        public virtual ICollection<Achievement> UserAchievements { get; set; } 
            = new HashSet<Achievement>();
    }

}
