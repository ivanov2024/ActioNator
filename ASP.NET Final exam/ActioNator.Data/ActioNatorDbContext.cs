using ActioNator.Data.EntityConfigurations;
using ActioNator.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using static ActioNator.Data.ActioNatorConnectionString;

namespace ActioNator.Data
{
    public class ActioNatorDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ActioNatorDbContext() { }

        public ActioNatorDbContext(DbContextOptions<ActioNatorDbContext> options)
            : base(options) { }

        public DbSet<Achievement> Achievements { get; set; } = null!;

        public DbSet<AchievementTemplate> AchievementTemplates { get; set; } = null!;

        public DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;

        public DbSet<UserLoginHistory> UserLoginHistories { get; set; } = null!;

        public DbSet<Chat> Chats { get; set; } = null!;

        public DbSet<Comment> Comments { get; set; } = null!;

        public DbSet<Exercise> Exercises { get; set; } = null!;

        public DbSet<ExerciseTemplate> ExerciseTemplates { get; set; } = null!;

        public DbSet<Goal> Goals { get; set; } = null!;

        public DbSet<JournalEntry> JournalEntries { get; set; } = null!;

        public DbSet<Message> Messages { get; set; } = null!;

        public DbSet<Post> Posts { get; set; } = null!;

        public DbSet<Workout> Workouts { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlServer(ConnectionString);
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base
                .OnModelCreating(modelBuilder);

            modelBuilder
                .ApplyConfigurationsFromAssembly(typeof(ApplicationUserConfiguration).Assembly);
        }
    }
}
