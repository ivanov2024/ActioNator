using System;
using System.Linq;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.JournalService;
using ActioNator.ViewModels.Journal;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace WebTests.Services
{
    public class JournalServiceTests
    {
        private TestInMemoryJournalDbContext _db = null!;
        private JournalService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var dbName = $"JournalDb_{Guid.NewGuid()}";
            _db = new TestInMemoryJournalDbContext(dbName);
            _service = new JournalService(_db);
        }

        [TearDown]
        public void TearDown()
        {
            _db?.Dispose();
        }

        [Test]
        public async Task CreateEntryAsync_Succeeds_AndMapsBack()
        {
            var vm = new JournalEntryViewModel
            {
                Title = "My Day",
                Content = "It was great!",
                MoodTag = "happy"
            };
            var userId = Guid.NewGuid();

            var result = await _service.CreateEntryAsync(vm, userId);

            Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(result.CreatedAt, Is.Not.EqualTo(default(DateTime)));

            var entity = await _db.JournalEntries.FirstOrDefaultAsync(e => e.Id == result.Id);
            Assert.That(entity, Is.Not.Null);
            Assert.That(entity!.UserId, Is.EqualTo(userId));
            Assert.That(entity.Title, Is.EqualTo("My Day"));
        }

        [Test]
        public void CreateEntryAsync_Validations()
        {
            var tooShort = new JournalEntryViewModel { Title = "aa" };
            var tooLong = new JournalEntryViewModel { Title = new string('a', 21) };
            var contentTooLong = new JournalEntryViewModel { Title = "ok", Content = new string('b', 151) };
            var moodTooLong = new JournalEntryViewModel { Title = "ok", MoodTag = new string('c', 51) };

            Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEntryAsync(tooShort, Guid.NewGuid()));
            Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEntryAsync(tooLong, Guid.NewGuid()));
            Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEntryAsync(contentTooLong, Guid.NewGuid()));
            Assert.ThrowsAsync<ArgumentException>(() => _service.CreateEntryAsync(moodTooLong, Guid.NewGuid()));
        }

        [Test]
        public async Task GetAllEntriesAsync_ReturnsOrdered_ByCreatedAtDesc()
        {
            var now = DateTime.UtcNow;
            await _db.JournalEntries.AddRangeAsync(
                new JournalEntry { Id = Guid.NewGuid(), Title = "1", Content = "a", CreatedAt = now.AddMinutes(-10) },
                new JournalEntry { Id = Guid.NewGuid(), Title = "2", Content = "b", CreatedAt = now.AddMinutes(-1) },
                new JournalEntry { Id = Guid.NewGuid(), Title = "3", Content = "c", CreatedAt = now.AddMinutes(-5) }
            );
            await _db.SaveChangesAsync();

            var list = (await _service.GetAllEntriesAsync()).ToList();
            Assert.That(list, Has.Count.EqualTo(3));
            Assert.That(list[0].Title, Is.EqualTo("2"));
            Assert.That(list[1].Title, Is.EqualTo("3"));
            Assert.That(list[2].Title, Is.EqualTo("1"));
        }

        [Test]
        public async Task GetEntryByIdAsync_ReturnsNull_WhenMissing()
        {
            var res = await _service.GetEntryByIdAsync(Guid.NewGuid());
            Assert.That(res, Is.Null);
        }

        [Test]
        public async Task UpdateEntryAsync_Updates_AndReturnsMapped()
        {
            var e = new JournalEntry
            {
                Id = Guid.NewGuid(),
                Title = "Old",
                Content = "X",
                CreatedAt = DateTime.UtcNow
            };
            await _db.JournalEntries.AddAsync(e);
            await _db.SaveChangesAsync();

            var input = new JournalEntryViewModel
            {
                Id = e.Id,
                Title = "New",
                Content = "Y",
                MoodTag = "ok"
            };

            var result = await _service.UpdateEntryAsync(input);

            Assert.That(result.Title, Is.EqualTo("New"));
            var inDb = await _db.JournalEntries.FindAsync(e.Id);
            Assert.That(inDb!.Title, Is.EqualTo("New"));
            Assert.That(inDb.Content, Is.EqualTo("Y"));
            Assert.That(inDb.MoodTag, Is.EqualTo("ok"));
        }

        [Test]
        public void UpdateEntryAsync_Throws_WhenNotFound()
        {
            var input = new JournalEntryViewModel
            {
                Id = Guid.NewGuid(),
                Title = "abc"
            };
            Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateEntryAsync(input));
        }

        [Test]
        public async Task DeleteEntryAsync_SetsSoftDelete_AndReturnsTrue()
        {
            var e = new JournalEntry { Id = Guid.NewGuid(), Title = "t", CreatedAt = DateTime.UtcNow };
            await _db.JournalEntries.AddAsync(e);
            await _db.SaveChangesAsync();

            var ok = await _service.DeleteEntryAsync(e.Id);
            Assert.That(ok, Is.True);

            var inDb = await _db.JournalEntries.IgnoreQueryFilters().FirstAsync(x => x.Id == e.Id);
            Assert.That(inDb.IsDeleted, Is.True);
        }

        [Test]
        public async Task DeleteEntryAsync_ReturnsFalse_WhenMissing()
        {
            var ok = await _service.DeleteEntryAsync(Guid.NewGuid());
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task SearchEntriesAsync_Filters_By_Title_Content_And_Mood()
        {
            await _db.JournalEntries.AddRangeAsync(
                new JournalEntry { Id = Guid.NewGuid(), Title = "Run", Content = "morning", MoodTag = "active", CreatedAt = DateTime.UtcNow },
                new JournalEntry { Id = Guid.NewGuid(), Title = "Cook", Content = "pasta", MoodTag = "relaxed", CreatedAt = DateTime.UtcNow }
            );
            await _db.SaveChangesAsync();

            var t = (await _service.SearchEntriesAsync("run")).ToList();
            var c = (await _service.SearchEntriesAsync("PASTA")).ToList();
            var m = (await _service.SearchEntriesAsync("lax")).ToList();

            Assert.That(t, Has.Count.EqualTo(1));
            Assert.That(c, Has.Count.EqualTo(1));
            Assert.That(m, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task SearchEntriesAsync_ReturnsAll_WhenTermEmpty()
        {
            await _db.JournalEntries.AddRangeAsync(
                new JournalEntry { Id = Guid.NewGuid(), Title = "A", CreatedAt = DateTime.UtcNow },
                new JournalEntry { Id = Guid.NewGuid(), Title = "B", CreatedAt = DateTime.UtcNow }
            );
            await _db.SaveChangesAsync();

            var all = (await _service.SearchEntriesAsync(" ")).ToList();
            Assert.That(all, Has.Count.EqualTo(2));
        }
    }
}
