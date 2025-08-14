using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WebTests.Services
{
    /// <summary>
    /// Minimal InMemory DbContext for CommunityService tests.
    /// Keeps users, posts, comments, likes, images and related reports.
    /// </summary>
    public sealed class TestInMemoryCommunityDbContext : ActioNatorDbContext
    {
        public TestInMemoryCommunityDbContext(string dbName)
            : base(new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            // EF InMemory provider does not support transactions; suppress the warning as an exception
            optionsBuilder.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure Identity and base configurations are applied
            base.OnModelCreating(modelBuilder);

            // Explicitly keep community-related entities
            modelBuilder.Entity<ApplicationUser>();
            modelBuilder.Entity<Post>();
            modelBuilder.Entity<PostImage>();
            modelBuilder.Entity<PostLike>();
            modelBuilder.Entity<Comment>();
            modelBuilder.Entity<CommentLike>();
            modelBuilder.Entity<PostReport>();
            modelBuilder.Entity<CommentReport>();

            // Ignore everything else to keep model minimal for these tests
            modelBuilder.Ignore<Achievement>();
            modelBuilder.Ignore<AchievementTemplate>();
            modelBuilder.Ignore<UserLoginHistory>();
            modelBuilder.Ignore<Chat>();
            modelBuilder.Ignore<Exercise>();
            modelBuilder.Ignore<ExerciseTemplate>();
            modelBuilder.Ignore<Goal>();
            modelBuilder.Ignore<JournalEntry>();
            modelBuilder.Ignore<Message>();
            modelBuilder.Ignore<Workout>();
        }
    }
}
