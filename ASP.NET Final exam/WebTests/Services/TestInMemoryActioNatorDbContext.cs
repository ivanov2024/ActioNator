using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    public sealed class TestInMemoryActioNatorDbContext : ActioNatorDbContext
    {
        public TestInMemoryActioNatorDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        public TestInMemoryActioNatorDbContext(DbContextOptions<ActioNatorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Start with base model to ensure Identity/EF infrastructure is initialized,
            // then slim it down to only what's needed for these tests.
            base.OnModelCreating(modelBuilder);

            // Only include Goal, ignore everything else from the base context
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Ignore<ApplicationUser>();
            modelBuilder.Ignore<UserLoginHistory>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Comment>();
            modelBuilder.Ignore<Exercise>();
            modelBuilder.Ignore<ExerciseTemplate>();
            modelBuilder.Ignore<JournalEntry>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<Post>();
            modelBuilder.Ignore<PostImage>();
            modelBuilder.Ignore<PostLike>();
            modelBuilder.Ignore<PostReport>();
            modelBuilder.Ignore<CommentReport>();
            modelBuilder.Ignore<CommentLike>();
            modelBuilder.Ignore<Workout>();

            modelBuilder.Entity<Goal>(goal =>
            {
                goal.HasKey(g => g.Id);
                goal.Property(g => g.Title).IsRequired(false);
                goal.Property(g => g.Description).IsRequired(false);
                goal.Property(g => g.CreatedAt).IsRequired();
                goal.Property(g => g.IsCompleted).HasDefaultValue(false);
                goal.Property(g => g.IsDeleted).HasDefaultValue(false);
                // Remove any relationship to ApplicationUser since it's ignored in this test context
                goal.Ignore(g => g.ApplicationUser);
                goal.HasQueryFilter(g => g.IsDeleted == false);
            });
        }
    }
}
