using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    /// <summary>
    /// Minimal InMemory DbContext for UserProfileService tests.
    /// Keeps only ApplicationUser entity and ignores the rest to avoid provider-specific configs.
    /// </summary>
    public sealed class TestInMemoryUserProfileDbContext : ActioNatorDbContext
    {
        public TestInMemoryUserProfileDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        public TestInMemoryUserProfileDbContext(DbContextOptions<ActioNatorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Start with base model to ensure Identity schema for ApplicationUser is configured
            base.OnModelCreating(modelBuilder);

            // Only include ApplicationUser, ignore everything else from the base context
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Entity<ApplicationUser>();
            modelBuilder.Ignore<UserLoginHistory>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Comment>();
            modelBuilder.Ignore<Exercise>();
            modelBuilder.Ignore<ExerciseTemplate>();
            modelBuilder.Ignore<Goal>();
            modelBuilder.Ignore<JournalEntry>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<Post>();
            modelBuilder.Ignore<PostImage>();
            modelBuilder.Ignore<PostLike>();
            modelBuilder.Ignore<PostReport>();
            modelBuilder.Ignore<CommentReport>();
            modelBuilder.Ignore<CommentLike>();
            modelBuilder.Ignore<Workout>();
        }
    }
}
