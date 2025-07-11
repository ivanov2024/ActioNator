using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class AchievementTemplateConfiguration : IEntityTypeConfiguration<AchievementTemplate>
    {
        public void Configure(EntityTypeBuilder<AchievementTemplate> achievementTemplate)
        {
            achievementTemplate
                .HasKey(at => at.Id);

            achievementTemplate
                .Property(a => a.ImageUrl)
                .HasDefaultValue("/images/achievement/medalLogo.png");
        }
    }
}
