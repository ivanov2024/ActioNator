using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    /// <summary>
    /// InMemory DbContext tailored for WorkoutService tests. Keeps only workout-related entities
    /// to avoid provider-specific configuration issues.
    /// </summary>
    public sealed class TestInMemoryWorkoutDbContext : ActioNatorDbContext
    {
        public TestInMemoryWorkoutDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        public TestInMemoryWorkoutDbContext(DbContextOptions<ActioNatorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure base mappings (including relationships) are configured
            base.OnModelCreating(modelBuilder);

            // Keep only entities needed for WorkoutService tests
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Ignore<ApplicationUser>();
            modelBuilder.Ignore<UserLoginHistory>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Comment>();
            modelBuilder.Ignore<Goal>();
            modelBuilder.Ignore<JournalEntry>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<Post>();
            modelBuilder.Ignore<PostImage>();
            modelBuilder.Ignore<PostLike>();
            modelBuilder.Ignore<PostReport>();
            modelBuilder.Ignore<CommentReport>();
            modelBuilder.Ignore<CommentLike>();
            // Keep: Workout, Exercise, ExerciseTemplate
        }
    }
}
