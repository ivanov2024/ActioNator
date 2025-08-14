using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.GoalService;
using ActioNator.ViewModels.Goal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    public class GoalServiceTests
    {
        private static ActioNatorDbContext CreateDb(string dbName)
        {
            // Use the simplified in-memory context to avoid provider-specific EnsureCreated issues
            return new TestInMemoryActioNatorDbContext(dbName);
        }

        private sealed class TestClock : IClock
        {
            public DateTime UtcNow { get; set; }
        }

        [Test]
        public async Task GetUserGoalsAsync_Filters_All_Active_Completed_Overdue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dbName = Guid.NewGuid().ToString();
            await using var db = CreateDb(dbName);

            var fixedNow = new DateTime(2024, 01, 10, 12, 0, 0, DateTimeKind.Utc);
            var clock = new TestClock { UtcNow = fixedNow };
            var logger = Mock.Of<ILogger<GoalService>>();

            try
            {
                db.Goals.AddRange(
                    new Goal { Id = Guid.NewGuid(), ApplicationUserId = userId, Title = "A1", IsCompleted = false, DueDate = fixedNow.AddDays(2), CreatedAt = fixedNow }, // active
                    new Goal { Id = Guid.NewGuid(), ApplicationUserId = userId, Title = "C1", IsCompleted = true, DueDate = fixedNow.AddDays(-1), CreatedAt = fixedNow },  // completed
                    new Goal { Id = Guid.NewGuid(), ApplicationUserId = userId, Title = "O1", IsCompleted = false, DueDate = fixedNow.AddDays(-1), CreatedAt = fixedNow }   // overdue
                );
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TestContext.Progress.WriteLine("Seeding failed: " + ex);
                if (ex.InnerException != null)
                {
                    TestContext.Progress.WriteLine("Inner: " + ex.InnerException);
                }
                Assert.Fail(ex.ToString());
            }

            var service = new GoalService(db, logger, clock);

            // Act
            var all = await service.GetUserGoalsAsync(userId, "all");
            var active = await service.GetUserGoalsAsync(userId, "active");
            var completed = await service.GetUserGoalsAsync(userId, "completed");
            var overdue = await service.GetUserGoalsAsync(userId, "overdue");

            // Assert
            Assert.That(all.Count, Is.EqualTo(3));
            Assert.That(active.Count, Is.EqualTo(2 - 1)); // only the non-completed future-due
            Assert.That(active.Any(g => g.Title == "A1"), Is.True);
            Assert.That(completed.Count, Is.EqualTo(1));
            Assert.That(overdue.Count, Is.EqualTo(1));
            Assert.That(overdue.Single().Title, Is.EqualTo("O1"));
        }

        [Test]
        public async Task CreateGoalAsync_Persists_And_Returns_Model_With_Id()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fixedNow = new DateTime(2024, 02, 01, 0, 0, 0, DateTimeKind.Utc);
            var clock = new TestClock { UtcNow = fixedNow };
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();
            var service = new GoalService(db, logger, clock);

            var model = new GoalViewModel
            {
                Title = "New Goal",
                Description = "Desc",
                DueDate = new DateTime(2024, 03, 01, 0, 0, 0, DateTimeKind.Utc),
                Completed = false
            };

            // Act
            var result = await service.CreateGoalAsync(model, userId);

            // Assert
            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            var entity = await db.Goals.SingleAsync(g => g.Id == result.Id);
            Assert.That(entity.Title, Is.EqualTo("New Goal"));
            Assert.That(entity.CreatedAt, Is.EqualTo(fixedNow));
            Assert.That(entity.DueDate.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(entity.ApplicationUserId, Is.EqualTo(userId));
        }

        [Test]
        public async Task CreateGoalAsync_EmptyUserId_Throws()
        {
            // Arrange
            var clock = new TestClock { UtcNow = DateTime.UtcNow };
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();
            var service = new GoalService(db, logger, clock);

            var model = new GoalViewModel { Title = "t", Description = "d", DueDate = DateTime.UtcNow };

            // Act + Assert
            Assert.ThrowsAsync<ArgumentException>(() => service.CreateGoalAsync(model, Guid.Empty));
        }

        [Test]
        public async Task UpdateGoalAsync_WhenOwner_Updates_And_Sets_CompletedAt()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var fixedNow = new DateTime(2024, 02, 01, 12, 0, 0, DateTimeKind.Utc);
            var clock = new TestClock { UtcNow = fixedNow };
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();

            var existing = new Goal
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = userId,
                Title = "Old",
                Description = "OldD",
                DueDate = fixedNow.AddDays(1),
                IsCompleted = false,
                CreatedAt = fixedNow
            };
            db.Goals.Add(existing);
            await db.SaveChangesAsync();

            var service = new GoalService(db, logger, clock);
            var update = new GoalViewModel
            {
                Id = existing.Id,
                Title = "New",
                Description = "NewD",
                DueDate = fixedNow.AddDays(2),
                Completed = true
            };

            // Act
            var result = await service.UpdateGoalAsync(update, userId);

            // Assert
            Assert.That(result.Title, Is.EqualTo("New"));
            var entity = await db.Goals.SingleAsync(g => g.Id == existing.Id);
            Assert.That(entity.Title, Is.EqualTo("New"));
            Assert.That(entity.Description, Is.EqualTo("NewD"));
            Assert.That(entity.IsCompleted, Is.True);
            Assert.That(entity.CompletedAt, Is.EqualTo(fixedNow));
        }

        [Test]
        public async Task UpdateGoalAsync_NotOwner_Throws()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();
            var clock = new TestClock { UtcNow = DateTime.UtcNow };

            var existing = new Goal
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = ownerId,
                Title = "t",
                Description = "d",
                DueDate = DateTime.UtcNow,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };
            db.Goals.Add(existing);
            await db.SaveChangesAsync();

            var service = new GoalService(db, logger, clock);
            var update = new GoalViewModel { Id = existing.Id, Title = "x", Description = "y", DueDate = DateTime.UtcNow, Completed = false };

            // Act + Assert
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateGoalAsync(update, otherUser));
        }

        [Test]
        public async Task DeleteGoalAsync_RemovesEntity()
        {
            // Arrange
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();
            var clock = new TestClock { UtcNow = DateTime.UtcNow };

            var g = new Goal { Id = Guid.NewGuid(), ApplicationUserId = Guid.NewGuid(), Title = "t", DueDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
            db.Goals.Add(g);
            await db.SaveChangesAsync();

            var service = new GoalService(db, logger, clock);

            // Act
            await service.DeleteGoalAsync(g.Id);

            // Assert
            Assert.That(await db.Goals.AnyAsync(x => x.Id == g.Id), Is.False);
        }

        [Test]
        public async Task ToggleGoalCompletionAsync_Toggles_And_SetsCompletedAt()
        {
            // Arrange
            var fixedNow = new DateTime(2024, 04, 01, 7, 0, 0, DateTimeKind.Utc);
            var clock = new TestClock { UtcNow = fixedNow };
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();

            var g = new Goal { Id = Guid.NewGuid(), ApplicationUserId = Guid.NewGuid(), Title = "t", DueDate = fixedNow.AddDays(1), IsCompleted = false, CreatedAt = fixedNow };
            db.Goals.Add(g);
            await db.SaveChangesAsync();

            var service = new GoalService(db, logger, clock);

            // Act 1: toggle to completed
            var vm1 = await service.ToggleGoalCompletionAsync(g.Id);
            Assert.That(vm1.Completed, Is.True);
            var entity1 = await db.Goals.SingleAsync(x => x.Id == g.Id);
            Assert.That(entity1.IsCompleted, Is.True);
            Assert.That(entity1.CompletedAt, Is.EqualTo(fixedNow));

            // Act 2: toggle back to active
            clock.UtcNow = fixedNow.AddHours(1);
            var vm2 = await service.ToggleGoalCompletionAsync(g.Id);
            Assert.That(vm2.Completed, Is.False);
            var entity2 = await db.Goals.SingleAsync(x => x.Id == g.Id);
            Assert.That(entity2.IsCompleted, Is.False);
            Assert.That(entity2.CompletedAt, Is.Null);
        }

        [Test]
        public async Task VerifyGoalOwnershipAsync_ReturnsExpected()
        {
            // Arrange
            var user = Guid.NewGuid();
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var logger = Mock.Of<ILogger<GoalService>>();
            var clock = new TestClock { UtcNow = DateTime.UtcNow };

            var g1 = new Goal { Id = Guid.NewGuid(), ApplicationUserId = user, Title = "mine", DueDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
            var g2 = new Goal { Id = Guid.NewGuid(), ApplicationUserId = Guid.NewGuid(), Title = "other", DueDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
            db.Goals.AddRange(g1, g2);
            await db.SaveChangesAsync();

            var service = new GoalService(db, logger, clock);

            // Act + Assert
            Assert.That(await service.VerifyGoalOwnershipAsync(g1.Id, user), Is.True);
            Assert.That(await service.VerifyGoalOwnershipAsync(g2.Id, user), Is.False);
            Assert.That(await service.VerifyGoalOwnershipAsync(g1.Id, null), Is.False);
        }
    }
}
