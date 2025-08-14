using System;
using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    // Relational in-memory DbContext for tests using SQLite
    public sealed class TestSqliteActioNatorDbContext : ActioNatorDbContext, IDisposable
    {
        private readonly SqliteConnection _connection;

        public TestSqliteActioNatorDbContext(string dbName)
            : base(BuildOptions(out var conn, dbName))
        {
            _connection = conn;
            Database.EnsureCreated();
        }

        private static DbContextOptions<ActioNatorDbContext> BuildOptions(out SqliteConnection connection, string name)
        {
            // Keep a single open connection alive for the in-memory database lifetime
            var connString = new SqliteConnectionStringBuilder
            {
                DataSource = $"file:{name}?mode=memory&cache=shared"
            }.ToString();
            connection = new SqliteConnection(connString);
            connection.Open();
            return new DbContextOptionsBuilder<ActioNatorDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Minimal model for tests: configure only Goal to avoid provider-specific defaults in other entities
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

        public new void Dispose()
        {
            base.Dispose();
            _connection?.Dispose();
        }
    }
}
