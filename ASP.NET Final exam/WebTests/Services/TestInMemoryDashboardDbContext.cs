using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    /// <summary>
    /// Minimal InMemory DbContext for UserDashboardService tests.
    /// Keeps users, goals, journal entries, workouts, posts, images, comments and login history.
    /// </summary>
    public sealed class TestInMemoryDashboardDbContext : ActioNatorDbContext
    {
        public TestInMemoryDashboardDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure Identity and base configurations are applied
            base.OnModelCreating(modelBuilder);

            // Explicitly keep dashboard-related entities
            modelBuilder.Entity<ApplicationUser>();
            modelBuilder.Entity<UserLoginHistory>();
            modelBuilder.Entity<Goal>();
            modelBuilder.Entity<JournalEntry>();
            modelBuilder.Entity<Workout>();
            modelBuilder.Entity<Post>();
            modelBuilder.Entity<PostImage>();
            modelBuilder.Entity<Comment>();

            // Ignore unrelated entities
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<Exercise>();
            modelBuilder.Ignore<ExerciseTemplate>();
            modelBuilder.Ignore<PostLike>();
            modelBuilder.Ignore<PostReport>();
            modelBuilder.Ignore<CommentReport>();
            modelBuilder.Ignore<CommentLike>();
        }
    }
}
