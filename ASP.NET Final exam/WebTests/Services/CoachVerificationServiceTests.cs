using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Data.Models.Enums;
using ActioNator.Services.Implementations.VerifyCoach;
using ActioNator.ViewModels.CoachVerification;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class CoachVerificationServiceTests
    {
        private TestInMemoryUserProfileDbContext _db = null!;
        private Mock<IWebHostEnvironment> _envMock = null!;
        private Mock<ILogger<CoachVerificationService>> _loggerMock = null!;
        private CoachVerificationService _service = null!;
        private string _tempRoot = null!;

        [SetUp]
        public void SetUp()
        {
            _db = new TestInMemoryUserProfileDbContext(Guid.NewGuid().ToString());
            _envMock = new Mock<IWebHostEnvironment>();
            _loggerMock = new Mock<ILogger<CoachVerificationService>>();

            _tempRoot = Path.Combine(Path.GetTempPath(), "ActioNatorTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempRoot);
            _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);

            _service = new CoachVerificationService(_db, _loggerMock.Object, _envMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_tempRoot))
                {
                    Directory.Delete(_tempRoot, true);
                }
            }
            catch { /* ignore cleanup failures */ }

            _db.Dispose();
        }

        [Test]
        public async Task GetPendingVerificationsCountAsync_CountsOnlyEligibleUsers()
        {
            var u1 = CreateUser(isDeleted: false, verified: false, hasDegree: true);
            var u2 = CreateUser(isDeleted: false, verified: true, hasDegree: true); // verified -> exclude
            var u3 = CreateUser(isDeleted: true, verified: false, hasDegree: true); // deleted -> exclude
            var u4 = CreateUser(isDeleted: false, verified: false, hasDegree: false); // no degree -> exclude
            _db.Users.AddRange(u1, u2, u3, u4);
            await _db.SaveChangesAsync();

            var count = await _service.GetPendingVerificationsCountAsync();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task GetDocumentsForUserAsync_ReturnsRelativePathsAndTypes_WhenFilesExist()
        {
            // Arrange
            var user = CreateUser();
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var userIdStr = user.Id.ToString();
            var userFolder = Path.Combine(_tempRoot, "App_Data", "coach-verifications", userIdStr);
            Directory.CreateDirectory(userFolder);

            var f1 = Path.Combine(userFolder, "doc1.pdf");
            var f2 = Path.Combine(userFolder, "img1.jpg");
            File.WriteAllText(f1, "pdf");
            File.WriteAllText(f2, "img");

            // Act
            var docs = await _service.GetDocumentsForUserAsync(userIdStr);

            // Assert
            Assert.That(docs, Has.Count.EqualTo(2));
            var rel1 = Path.Combine("App_Data", "coach-verifications", userIdStr, "doc1.pdf");
            var rel2 = Path.Combine("App_Data", "coach-verifications", userIdStr, "img1.jpg");
            Assert.That(docs.Any(d => d.RelativePath == rel1 && d.FileType == "pdf"));
            Assert.That(docs.Any(d => d.RelativePath == rel2 && d.FileType == "image"));
        }

        [Test]
        public async Task GetDocumentsForUserAsync_InvalidUserId_ReturnsEmpty()
        {
            var docs = await _service.GetDocumentsForUserAsync("not-a-guid");
            Assert.That(docs, Is.Empty);
        }

        [Test]
        public async Task GetDocumentsForUserAsync_UnknownUser_ReturnsEmpty()
        {
            var docs = await _service.GetDocumentsForUserAsync(Guid.NewGuid().ToString());
            Assert.That(docs, Is.Empty);
        }

        [Test]
        public async Task GetAllVerificationRequestsAsync_ListsOnlyNonVerifiedUsersWithDocs()
        {
            // Arrange users
            var pendingUser = CreateUser(verified: false);
            var verifiedUser = CreateUser(verified: true);
            _db.Users.AddRange(pendingUser, verifiedUser);
            await _db.SaveChangesAsync();

            // FS layout
            var root = Path.Combine(_tempRoot, "App_Data", "coach-verifications");
            Directory.CreateDirectory(root);
            // Folder for non-verified user
            var pendingFolder = Path.Combine(root, pendingUser.Id.ToString());
            Directory.CreateDirectory(pendingFolder);
            File.WriteAllText(Path.Combine(pendingFolder, "degree.pdf"), "x");
            // Folder for verified user -> should be skipped
            var verifiedFolder = Path.Combine(root, verifiedUser.Id.ToString());
            Directory.CreateDirectory(verifiedFolder);
            File.WriteAllText(Path.Combine(verifiedFolder, "proof.jpg"), "x");
            // Non-GUID folder -> skipped
            Directory.CreateDirectory(Path.Combine(root, "not-a-guid"));
            // GUID folder with no matching user -> skipped
            Directory.CreateDirectory(Path.Combine(root, Guid.NewGuid().ToString()));

            // Act
            var requests = await _service.GetAllVerificationRequestsAsync();

            // Assert
            Assert.That(requests, Has.Count.EqualTo(1));
            var req = requests[0];
            Assert.That(req.UserId, Is.EqualTo(pendingUser.Id.ToString()));
            Assert.That(req.Documents, Is.Not.Empty);
            Assert.That(req.Documents[0], Is.TypeOf<CoachDocumentViewModel>());
        }

        [Test]
        public async Task ApproveVerificationAsync_SetsFlagsAndDeletesFolder()
        {
            // Arrange
            var user = CreateUser(verified: false, hasDegree: true);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            var userIdStr = user.Id.ToString();
            var folder = Path.Combine(_tempRoot, "App_Data", "coach-verifications", userIdStr);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "a.pdf"), "x");

            // Act
            var ok = await _service.ApproveVerificationAsync(userIdStr);

            // Assert
            Assert.That(ok, Is.True);
            var refreshed = await _db.Users.FindAsync(user.Id);
            Assert.That(refreshed!.IsVerifiedCoach, Is.True);
            Assert.That(refreshed.Role, Is.EqualTo(Role.Coach));
            Assert.That(refreshed.CoachDegreeFilePath, Is.Null);
            Assert.That(Directory.Exists(folder), Is.False);
        }

        [Test]
        public async Task ApproveVerificationAsync_InvalidGuid_ReturnsFalse()
        {
            var ok = await _service.ApproveVerificationAsync("bad-guid");
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task ApproveVerificationAsync_UnknownUser_ReturnsFalse()
        {
            var ok = await _service.ApproveVerificationAsync(Guid.NewGuid().ToString());
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task RejectVerificationAsync_ClearsDegreePathAndDeletesFolder()
        {
            // Arrange
            var user = CreateUser(verified: false, hasDegree: true);
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            var userIdStr = user.Id.ToString();
            var folder = Path.Combine(_tempRoot, "App_Data", "coach-verifications", userIdStr);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "b.jpg"), "x");

            // Act
            var ok = await _service.RejectVerificationAsync(userIdStr, "not_valid");

            // Assert
            Assert.That(ok, Is.True);
            var refreshed = await _db.Users.FindAsync(user.Id);
            Assert.That(refreshed!.CoachDegreeFilePath, Is.Null);
            Assert.That(Directory.Exists(folder), Is.False);
        }

        [Test]
        public async Task RejectVerificationAsync_InvalidGuid_ReturnsFalse()
        {
            var ok = await _service.RejectVerificationAsync("bad-guid", "reason");
            Assert.That(ok, Is.False);
        }

        [Test]
        public async Task RejectVerificationAsync_UnknownUser_ReturnsFalse()
        {
            var ok = await _service.RejectVerificationAsync(Guid.NewGuid().ToString(), "reason");
            Assert.That(ok, Is.False);
        }

        private static ApplicationUser CreateUser(bool isDeleted = false, bool verified = false, bool hasDegree = false)
        {
            var id = Guid.NewGuid();
            return new ApplicationUser
            {
                Id = id,
                UserName = $"user_{id}@ex.com",
                NormalizedUserName = $"USER_{id}@EX.COM",
                Email = $"user_{id}@ex.com",
                NormalizedEmail = $"USER_{id}@EX.COM",
                FirstName = "F",
                LastName = "L",
                ProfilePictureUrl = "http://example/avatar.png",
                RegisteredAt = DateTime.UtcNow,
                IsDeleted = isDeleted,
                IsVerifiedCoach = verified,
                Role = verified ? Role.Coach : Role.User,
                CoachDegreeFilePath = hasDegree ? "some/path" : null
            };
        }
    }
}
