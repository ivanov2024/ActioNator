using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    public sealed class TestInMemoryJournalDbContext : ActioNatorDbContext
    {
        public TestInMemoryJournalDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Only include JournalEntry to keep model minimal
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Ignore<ApplicationUser>();
            modelBuilder.Ignore<UserLoginHistory>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Comment>();
            modelBuilder.Ignore<Exercise>();
            modelBuilder.Ignore<ExerciseTemplate>();
            modelBuilder.Ignore<Goal>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<Post>();
            modelBuilder.Ignore<PostImage>();
            modelBuilder.Ignore<PostLike>();
            modelBuilder.Ignore<PostReport>();
            modelBuilder.Ignore<CommentReport>();
            modelBuilder.Ignore<CommentLike>();
            modelBuilder.Ignore<Workout>();

            modelBuilder.Entity<JournalEntry>(e =>
            {
                e.HasKey(j => j.Id);
                e.Property(j => j.Title).IsRequired();
                e.Property(j => j.Content).IsRequired(false);
                e.Property(j => j.MoodTag).IsRequired(false);
                e.Property(j => j.CreatedAt).IsRequired();
                e.Property(j => j.IsDeleted).HasDefaultValue(false);
                // Ignore navigation to ApplicationUser since it's not part of this minimal model
                e.Ignore(j => j.ApplicationUser);
                e.HasQueryFilter(j => j.IsDeleted == false);
            });
        }
    }
}
