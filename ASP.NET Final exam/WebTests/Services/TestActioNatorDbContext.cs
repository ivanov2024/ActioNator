using System;
using ActioNator.Data;
using ActioNator.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WebTests.Services
{
    // Test-only DbContext that forces InMemory provider to avoid SQL Server configuration
    public class TestActioNatorDbContext : ActioNatorDbContext
    {
        public TestActioNatorDbContext(DbContextOptions<ActioNatorDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Do NOT call base to avoid loading full application configurations that depend on SQL Server specifics
            modelBuilder.Entity<Goal>(goal =>
            {
                goal.HasKey(g => g.Id);
                goal.Property(g => g.Title).IsRequired(false);
                goal.Property(g => g.Description).IsRequired(false);
                goal.Property(g => g.CreatedAt).IsRequired();
                goal.Property(g => g.IsCompleted).HasDefaultValue(false);
                goal.Property(g => g.IsDeleted).HasDefaultValue(false);
                goal.HasQueryFilter(g => g.IsDeleted == false);
            });
        }
    }
}
