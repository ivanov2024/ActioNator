using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    /// <summary>
    /// Minimal InMemory DbContext for ReportReviewService tests.
    /// Keeps only entities related to moderation (users, posts, comments, and their reports).
    /// </summary>
    public sealed class TestInMemoryModerationDbContext : ActioNatorDbContext
    {
        public TestInMemoryModerationDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        public TestInMemoryModerationDbContext(DbContextOptions<ActioNatorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure Identity and base configurations are applied
            base.OnModelCreating(modelBuilder);

            // Explicitly keep moderation-related entities
            modelBuilder.Entity<ApplicationUser>();
            modelBuilder.Entity<Post>();
            modelBuilder.Entity<Comment>();
            modelBuilder.Entity<PostReport>();
            modelBuilder.Entity<CommentReport>();

            // Ignore everything else to avoid provider-specific configs
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Ignore<UserLoginHistory>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Exercise>();
            modelBuilder.Ignore<ExerciseTemplate>();
            modelBuilder.Ignore<Goal>();
            modelBuilder.Ignore<JournalEntry>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<PostImage>();
            modelBuilder.Ignore<PostLike>();
            modelBuilder.Ignore<CommentLike>();
            modelBuilder.Ignore<Workout>();
        }
    }
}
