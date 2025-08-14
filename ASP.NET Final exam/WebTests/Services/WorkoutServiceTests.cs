using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.WorkoutService;
using ActioNator.ViewModels.Workout;
using ActioNator.ViewModels.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class WorkoutServiceTests
    {
        private static TestInMemoryWorkoutDbContext CreateDb()
            => new TestInMemoryWorkoutDbContext(Guid.NewGuid().ToString());

        private static WorkoutService CreateService(TestInMemoryWorkoutDbContext db)
        {
            var logger = new Mock<ILogger<WorkoutService>>(MockBehavior.Loose);
            return new WorkoutService(db, logger.Object);
        }

        private static (Guid userId, ExerciseTemplate template) SeedTemplate(TestInMemoryWorkoutDbContext db)
        {
            var userId = Guid.NewGuid();
            var template = new ExerciseTemplate
            {
                Id = Guid.NewGuid(),
                Name = "Push Up",
                TargetedMuscle = "Chest",
                ImageUrl = "img"
            };
            db.ExerciseTemplates.Add(template);
            db.SaveChanges();
            return (userId, template);
        }

        private static Workout SeedWorkout(TestInMemoryWorkoutDbContext db, Guid userId, DateTime date, string? title = "W1")
        {
            var w = new Workout
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Date = date,
                Duration = TimeSpan.Zero,
                Exercises = new List<Exercise>()
            };
            db.Workouts.Add(w);
            db.SaveChanges();
            return w;
        }

        private static Exercise SeedExercise(TestInMemoryWorkoutDbContext db, Workout w, ExerciseTemplate t, int mins, string? notes = null)
        {
            var e = new Exercise
            {
                Id = Guid.NewGuid(),
                WorkoutId = w.Id,
                ExerciseTemplateId = t.Id,
                Sets = 3,
                Reps = 10,
                Weight = 0,
                Notes = notes,
                Duration = TimeSpan.FromMinutes(mins)
            };
            db.Exercises.Add(e);
            db.SaveChanges();
            return e;
        }

        [Test]
        public async Task GetWorkoutsPageAsync_Returns_Paged_And_Total()
        {
            using var db = CreateDb();
            var (userId, template) = SeedTemplate(db);
            // Seed 5 workouts on different dates
            for (int i = 0; i < 5; i++)
            {
                SeedWorkout(db, userId, DateTime.UtcNow.AddDays(-i), $"W{i}");
            }
            var sut = CreateService(db);

            var (page1, total1) = await sut.GetWorkoutsPageAsync(userId, page: 1, pageSize: 2);
            var (page2, total2) = await sut.GetWorkoutsPageAsync(userId, page: 2, pageSize: 2);

            Assert.That(total1, Is.EqualTo(5));
            Assert.That(total2, Is.EqualTo(5));
            Assert.That(page1.Count(), Is.EqualTo(2));
            Assert.That(page2.Count(), Is.EqualTo(2));
            // Ordered desc by Date; W0 newest, then W1
            Assert.That(page1.First().Title, Is.EqualTo("W0"));
            Assert.That(page2.First().Title, Is.EqualTo("W2"));
        }

        [Test]
        public async Task GetAllWorkoutsAsync_Returns_All_For_User()
        {
            using var db = CreateDb();
            var (user1, _) = SeedTemplate(db);
            var user2 = Guid.NewGuid();
            SeedWorkout(db, user1, DateTime.UtcNow, "U1-1");
            SeedWorkout(db, user1, DateTime.UtcNow.AddDays(-1), "U1-2");
            SeedWorkout(db, user2, DateTime.UtcNow, "U2-1");
            var sut = CreateService(db);

            var all = await sut.GetAllWorkoutsAsync(user1);
            Assert.That(all.Count(), Is.EqualTo(2));
            Assert.That(all.All(w => w.Title.StartsWith("U1")), Is.True);
        }

        [Test]
        public async Task GetWorkoutByIdAsync_Returns_Null_When_Not_Found()
        {
            using var db = CreateDb();
            var (userId, _) = SeedTemplate(db);
            var sut = CreateService(db);
            var res = await sut.GetWorkoutByIdAsync(Guid.NewGuid(), userId);
            Assert.That(res, Is.Null);
        }

        [Test]
        public async Task CreateWorkoutAsync_Creates_And_Maps_ViewModel()
        {
            using var db = CreateDb();
            var (userId, _) = SeedTemplate(db);
            var sut = CreateService(db);

            var vm = new WorkoutCardViewModel
            {
                Title = "Morning",
                Date = DateTime.UtcNow,
                Notes = "n"
            };

            var created = await sut.CreateWorkoutAsync(vm, userId);
            Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.Title, Is.EqualTo("Morning"));
            Assert.That(created.IsCompleted, Is.False);
            Assert.That(db.Workouts.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateWorkoutAsync_InvalidParams_Throw()
        {
            using var db = CreateDb();
            var sut = CreateService(db);
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.CreateWorkoutAsync(null!, Guid.NewGuid()));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.CreateWorkoutAsync(new WorkoutCardViewModel { Title = "t", Date = DateTime.UtcNow }, null));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.CreateWorkoutAsync(new WorkoutCardViewModel { Title = " ", Date = DateTime.UtcNow }, Guid.NewGuid()));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.CreateWorkoutAsync(new WorkoutCardViewModel { Title = "t" }, Guid.NewGuid()));
        }

        [Test]
        public async Task UpdateWorkoutAsync_Updates_Fields_And_Toggles_Completion_And_Recomputes_Duration()
        {
            using var db = CreateDb();
            var (userId, template) = SeedTemplate(db);
            var w = SeedWorkout(db, userId, DateTime.UtcNow, "Old");
            // add two exercises: 10 and 5 mins, one soft-deleted
            var e1 = SeedExercise(db, w, template, 10);
            var e2 = SeedExercise(db, w, template, 5);
            e2.IsDeleted = true; db.SaveChanges();

            var sut = CreateService(db);
            var update = new WorkoutCardViewModel
            {
                Id = w.Id,
                Title = "New",
                Notes = "X",
                Date = DateTime.UtcNow.AddDays(1),
                IsCompleted = true
            };

            var updated = await sut.UpdateWorkoutAsync(update, userId);
            Assert.That(updated.Title, Is.EqualTo("New"));
            Assert.That(updated.IsCompleted, Is.True);
            // Duration should be sum of non-deleted => 10 mins
            var entity = db.Workouts.First(x => x.Id == w.Id);
            Assert.That(entity.Duration, Is.EqualTo(TimeSpan.FromMinutes(10)));

            // Un-complete
            update.IsCompleted = false;
            updated = await sut.UpdateWorkoutAsync(update, userId);
            Assert.That(updated.IsCompleted, Is.False);
            Assert.That(db.Workouts.First(x => x.Id == w.Id).CompletedAt, Is.Null);
        }

        [Test]
        public void UpdateWorkoutAsync_InvalidParams_Throw()
        {
            using var db = CreateDb();
            var sut = CreateService(db);
            var bad = new WorkoutCardViewModel { Id = Guid.Empty };
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.UpdateWorkoutAsync(bad, null));
        }

        [Test]
        public async Task DeleteWorkoutAsync_SoftDeletes_And_ReturnsTrue_When_Owned()
        {
            using var db = CreateDb();
            var (userId, _) = SeedTemplate(db);
            var w = SeedWorkout(db, userId, DateTime.UtcNow);
            var sut = CreateService(db);

            var ok = await sut.DeleteWorkoutAsync(w.Id, userId);
            Assert.That(ok, Is.True);
            Assert.That(db.Workouts.IgnoreQueryFilters().First(x => x.Id == w.Id).IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteWorkoutAsync_ReturnsFalse_When_NotFound()
        {
            using var db = CreateDb();
            var sut = CreateService(db);
            var ok = await sut.DeleteWorkoutAsync(Guid.NewGuid(), Guid.NewGuid());
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task AddExerciseAsync_Adds_And_Recalculates_Duration()
        {
            using var db = CreateDb();
            var (userId, template) = SeedTemplate(db);
            var w = SeedWorkout(db, userId, DateTime.UtcNow);
            var sut = CreateService(db);

            var add = new ExerciseViewModel
            {
                WorkoutId = w.Id,
                ExerciseTemplateId = template.Id,
                Sets = 4,
                Reps = 8,
                Weight = 20,
                Notes = "n",
                Duration = 12
            };

            var created = await sut.AddExerciseAsync(add, userId);
            Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(db.Workouts.First(x => x.Id == w.Id).Duration, Is.EqualTo(TimeSpan.FromMinutes(12)));
            Assert.That(created.Name, Is.EqualTo("Push Up"));
        }

        [Test]
        public async Task UpdateExerciseAsync_Updates_And_Recalculates()
        {
            using var db = CreateDb();
            var (userId, template) = SeedTemplate(db);
            var w = SeedWorkout(db, userId, DateTime.UtcNow);
            var e = SeedExercise(db, w, template, 5);
            var sut = CreateService(db);

            var upd = new ExerciseViewModel
            {
                Id = e.Id,
                WorkoutId = w.Id,
                ExerciseTemplateId = template.Id,
                Sets = 5,
                Reps = 12,
                Weight = 30,
                Notes = "u",
                Duration = 20
            };

            var updated = await sut.UpdateExerciseAsync(upd, userId);
            Assert.That(updated.Duration, Is.EqualTo(20));
            Assert.That(db.Workouts.First(x => x.Id == w.Id).Duration, Is.EqualTo(TimeSpan.FromMinutes(20)));
        }

        [Test]
        public async Task DeleteExerciseAsync_SoftDeletes_And_Recalculates()
        {
            using var db = CreateDb();
            var (userId, template) = SeedTemplate(db);
            var w = SeedWorkout(db, userId, DateTime.UtcNow);
            var e1 = SeedExercise(db, w, template, 10);
            var e2 = SeedExercise(db, w, template, 7);
            var sut = CreateService(db);

            var ok = await sut.DeleteExerciseAsync(e2.Id, userId);
            Assert.That(ok, Is.True);
            Assert.That(db.Exercises.IgnoreQueryFilters().First(x => x.Id == e2.Id).IsDeleted, Is.True);
            Assert.That(db.Workouts.First(x => x.Id == w.Id).Duration, Is.EqualTo(TimeSpan.FromMinutes(10)));
        }

        [Test]
        public async Task GetExerciseTemplatesAsync_Returns_Templates()
        {
            using var db = CreateDb();
            SeedTemplate(db);
            var sut = CreateService(db);
            var list = await sut.GetExerciseTemplatesAsync();
            Assert.That(list.Count(), Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public async Task VerifyOwnership_Methods_Work_As_Expected()
        {
            using var db = CreateDb();
            var (userId, template) = SeedTemplate(db);
            var otherUser = Guid.NewGuid();
            var w = SeedWorkout(db, userId, DateTime.UtcNow);
            var e = SeedExercise(db, w, template, 5);
            var sut = CreateService(db);

            Assert.That(await sut.VerifyWorkoutOwnershipAsync(w.Id, userId), Is.True);
            Assert.That(await sut.VerifyWorkoutOwnershipAsync(w.Id, otherUser), Is.False);

            Assert.That(await sut.VerifyExerciseOwnershipAsync(e.Id, userId), Is.True);
            Assert.That(await sut.VerifyExerciseOwnershipAsync(e.Id, otherUser), Is.False);
        }

        [Test]
        public void GuardClauses_Throw_On_Invalid_UserId_Or_Ids()
        {
            using var db = CreateDb();
            var sut = CreateService(db);
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.GetWorkoutsPageAsync(Guid.Empty, 1, 10));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.GetAllWorkoutsAsync(Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.GetWorkoutByIdAsync(Guid.NewGuid(), Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.DeleteWorkoutAsync(Guid.Empty, Guid.NewGuid()));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.DeleteWorkoutAsync(Guid.NewGuid(), Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.AddExerciseAsync(new ExerciseViewModel { WorkoutId = Guid.NewGuid() }, Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.UpdateExerciseAsync(new ExerciseViewModel { Id = Guid.NewGuid(), WorkoutId = Guid.NewGuid() }, Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.VerifyWorkoutOwnershipAsync(Guid.NewGuid(), Guid.Empty));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.VerifyExerciseOwnershipAsync(Guid.NewGuid(), Guid.Empty));
        }
    }
}
