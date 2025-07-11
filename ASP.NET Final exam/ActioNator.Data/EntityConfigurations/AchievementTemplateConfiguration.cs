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

            achievementTemplate
                .HasData(new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Title = "Welcome Aboard",
                    Description = "You've successfully joined ActioNator!"
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Title = "First Goal Set",
                    Description = "You've set your first personal goal."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Title = "First Workout Logged",
                    Description = "You completed your first workout."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    Title = "Consistency Is Key",
                    Description = "You've logged activity for 7 consecutive days."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    Title = "Journal Initiate",
                    Description = "You wrote your first journal entry."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                    Title = "Community Voice",
                    Description = "You've created your first post."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
                    Title = "Supportive Soul",
                    Description = "You've commented on 5 posts or journal entries."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000008"),
                    Title = "10 Goals Completed",
                    Description = "You've successfully completed 10 goals."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000009"),
                    Title = "50 Workouts Strong",
                    Description = "You've logged 50 workouts. Amazing consistency!"
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    Title = "Coach Verified",
                    Description = "You have been verified as a certified coach."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                    Title = "Early Riser",
                    Description = "Completed a workout before 7 AM."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000012"),
                    Title = "Workout Hero",
                    Description = "Completed 100 workouts."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000013"),
                    Title = "Mindful Moment",
                    Description = "Wrote 10 consecutive journal entries."
                },
                new AchievementTemplate
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000014"),
                    Title = "Social Butterfly",
                    Description = "Connected with 5 new users."
                }
                );
        }
    }
}
