using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActioNator.Data.EntityConfigurations
{
    internal class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
    {
        public void Configure(EntityTypeBuilder<Achievement> achievement)
        {
            achievement
                .HasKey(a => a.Id);

            achievement
                .HasOne(a => a.ApplicationUser)
                .WithMany(ap => ap.Achievements)
                .HasForeignKey(a => a.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            achievement
                .HasOne(a => a.AchievementTemplate)
                .WithMany(ap => ap.UserAchievements)
                .HasForeignKey(a => a.AchievementTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            achievement
                .Property(a => a.IsActive)
                .HasDefaultValue(false);
        }
    }
}
