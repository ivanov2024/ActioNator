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
                .OnDelete(DeleteBehavior.Restrict);

            achievement
                .Property(a => a.ImageUrl)
                .HasDefaultValue("/images/achievement/medalLogo.png");

            achievement
                .Property(a => a.IsActive)
                .HasDefaultValue(false);
        }
    }
}
