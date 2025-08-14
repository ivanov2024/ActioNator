using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.UserDashboard;
using ActioNator.ViewModels.Dashboard;
using ActioNator.ViewModels.Posts;
using ActioNator.ViewModels.Workouts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class UserDashboardServiceTests
    {
        private static TestInMemoryDashboardDbContext CreateDb(string name)
            => new TestInMemoryDashboardDbContext($"db_dashboard_{name}_{Guid.NewGuid()}");

        private static (ApplicationUser user, Guid userId) SeedUser(TestInMemoryDashboardDbContext db)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "john.doe",
                Email = "john@example.com",
                FirstName = "John",
                LastName = "Doe",
                ProfilePictureUrl = "http://img/profile.jpg",
                LastLoginAt = DateTime.UtcNow.AddDays(-1),
            };
            db.Users.Add(user);
            db.SaveChanges();
            return (user, user.Id);
        }

        private static void SeedGoals(TestInMemoryDashboardDbContext db, Guid userId)
        {
            db.Goals.AddRange(new[]
            {
                new Goal{ Id = Guid.NewGuid(), ApplicationUserId = userId, Title = "A", Description = "desc A", CreatedAt = DateTime.UtcNow.AddDays(-10), IsCompleted = false },
                new Goal{ Id = Guid.NewGuid(), ApplicationUserId = userId, Title = "B", Description = "desc B", CreatedAt = DateTime.UtcNow.AddDays(-5), IsCompleted = false },
                new Goal{ Id = Guid.NewGuid(), ApplicationUserId = userId, Title = "C", Description = "desc C", CreatedAt = DateTime.UtcNow.AddDays(-1), IsCompleted = true },
            });
            db.SaveChanges();
        }

        private static void SeedJournalEntries(TestInMemoryDashboardDbContext db, ApplicationUser user)
        {
            db.JournalEntries.AddRange(new[]
            {
                new JournalEntry{ Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Title = "T1", Content = "J1", CreatedAt = DateTime.UtcNow.AddDays(-3) },
                new JournalEntry{ Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Title = "T2", Content = "J2", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new JournalEntry{ Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Title = "T3", Content = "J3", CreatedAt = DateTime.UtcNow.AddDays(-1) },
            });
            db.SaveChanges();
        }

        private static void SeedWorkouts(TestInMemoryDashboardDbContext db, Guid userId)
        {
            db.Workouts.AddRange(new[]
            {
                new Workout{ Id = Guid.NewGuid(), UserId = userId, Title = "W1", Duration = TimeSpan.FromMinutes(30), CompletedAt = DateTime.UtcNow.AddDays(-1) },
                new Workout{ Id = Guid.NewGuid(), UserId = userId, Title = "W2", Duration = TimeSpan.FromMinutes(20), CompletedAt = DateTime.UtcNow.AddDays(-2) },
                new Workout{ Id = Guid.NewGuid(), UserId = userId, Title = "W3", Duration = TimeSpan.FromMinutes(10), CompletedAt = DateTime.UtcNow.AddDays(-3) },
                new Workout{ Id = Guid.NewGuid(), UserId = userId, Title = "W4", Duration = TimeSpan.FromMinutes(5), CompletedAt = DateTime.UtcNow.AddDays(-4) },
            });
            db.SaveChanges();
        }

        private static void SeedPosts(TestInMemoryDashboardDbContext db, ApplicationUser user)
        {
            var p1 = new Post { Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Content = "P1", CreatedAt = DateTime.UtcNow.AddMinutes(-30), LikesCount = 1, SharesCount = 0, IsPublic = true };
            var p2 = new Post { Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Content = "P2", CreatedAt = DateTime.UtcNow.AddHours(-3), LikesCount = 2, SharesCount = 1, IsPublic = true };
            var p3 = new Post { Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Content = "P3", CreatedAt = DateTime.UtcNow.AddDays(-1), LikesCount = 3, SharesCount = 2, IsPublic = true };
            var p4 = new Post { Id = Guid.NewGuid(), UserId = user.Id, ApplicationUser = user, Content = "P4", CreatedAt = DateTime.UtcNow.AddDays(-2), LikesCount = 4, SharesCount = 3, IsPublic = true };
            db.Posts.AddRange(p1, p2, p3, p4);

            db.Comments.AddRange(new[]
            {
                new Comment{ Id = Guid.NewGuid(), PostId = p1.Id, Content = "c1", AuthorId = user.Id, Author = user, CreatedAt = DateTime.UtcNow.AddMinutes(-10), IsDeleted = false },
                new Comment{ Id = Guid.NewGuid(), PostId = p2.Id, Content = "c2", AuthorId = user.Id, Author = user, CreatedAt = DateTime.UtcNow.AddMinutes(-20), IsDeleted = true },
                new Comment{ Id = Guid.NewGuid(), PostId = p3.Id, Content = "c3", AuthorId = user.Id, Author = user, CreatedAt = DateTime.UtcNow.AddMinutes(-40), IsDeleted = false },
            });
            db.SaveChanges();
        }

        private static void SeedLoginHistory(TestInMemoryDashboardDbContext db, Guid userId)
        {
            var today = DateTime.Today;
            db.UserLoginHistories.AddRange(new[]
            {
                new UserLoginHistory{ Id = Guid.NewGuid(), UserId = userId, LoginDate = today.AddDays(-1) },
                new UserLoginHistory{ Id = Guid.NewGuid(), UserId = userId, LoginDate = today.AddDays(-2) },
                new UserLoginHistory{ Id = Guid.NewGuid(), UserId = userId, LoginDate = today.AddDays(-4) },
            });
            db.SaveChanges();
        }

        [Test]
        public async Task GetDashboardDataAsync_ReturnsAggregatedData()
        {
            using var db = CreateDb(nameof(GetDashboardDataAsync_ReturnsAggregatedData));
            var (user, userId) = SeedUser(db);
            SeedGoals(db, userId);
            SeedJournalEntries(db, user);
            SeedWorkouts(db, userId);
            SeedPosts(db, user);
            SeedLoginHistory(db, userId);

            var sut = new UserDashboardService(db);
            DashboardViewModel result = await sut.GetDashboardDataAsync(userId, user);

            Assert.That(result.UserName, Is.EqualTo("John Doe"));
            Assert.That(result.ActiveGoalsCount, Is.EqualTo(2));
            Assert.That(result.JournalEntriesCount, Is.EqualTo(3));
            Assert.That(result.CurrentStreakCount, Is.EqualTo(2)); // yesterday and day before

            // Workouts: top 3 most recent
            Assert.That(result.RecentWorkouts, Is.Not.Null);
            Assert.That(result.RecentWorkouts.Count(), Is.EqualTo(3));
            var wTitles = result.RecentWorkouts.Select(w => w.Title).ToList();
            CollectionAssert.AreEqual(new[] {"W1","W2","W3"}, wTitles);

            // Posts: top 3 most recent
            Assert.That(result.RecentPosts, Is.Not.Null);
            var posts = result.RecentPosts.ToList();
            Assert.That(posts.Count, Is.EqualTo(3));
            CollectionAssert.AreEqual(new[] {"P1","P2","P3"}, posts.Select(p => p.Content).ToArray());
            // CommentsCount excludes soft-deleted
            var p1 = posts.First();
            Assert.That(p1.CommentsCount, Is.EqualTo(1));
            Assert.That(p1.AuthorName, Is.EqualTo("john.doe"));
            Assert.That(p1.ProfilePictureUrl, Is.EqualTo("http://img/profile.jpg"));
            Assert.That(p1.IsAuthor, Is.True);
        }
    }
}
