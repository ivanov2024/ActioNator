using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.UserProfileService;
using ActioNator.Services.Interfaces.FileServices;
using FinalExamUI.ViewModels.UserProfile;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    public class UserProfileServiceTests
    {
        private string _originalCwd = null!;

        [SetUp]
        public void SetUp()
        {
            _originalCwd = Directory.GetCurrentDirectory();
        }

        private static ActioNatorDbContext CreateDb(string name)
        {
            return new TestInMemoryUserProfileDbContext(name);
        }

        private static UserProfileService CreateService(ActioNatorDbContext db)
        {
            var fs = new Mock<IFileSystem>(MockBehavior.Strict); // not used by implementation but required by ctor
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(MockBehavior.Strict);
            return new UserProfileService(fs.Object, env.Object, db);
        }

        private static (string BaseDir, Action Cleanup) UseIsolatedWorkingDir()
        {
            var baseDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "UserProfileServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(baseDir);
            var prev = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(baseDir);
            void cleanup()
            {
                try
                {
                    Directory.SetCurrentDirectory(prev);
                }
                catch { /* ignore */ }
                try
                {
                    if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
                }
                catch { /* ignore */ }
            }
            return (baseDir, cleanup);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.SetCurrentDirectory(_originalCwd);
        }

        [Test]
        public async Task GetUserProfileAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var service = CreateService(db);

            // Act
            var result = await service.GetUserProfileAsync(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetUserProfileAsync_UserFound_MapsFields_AndLoadsFriendsTab()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                ProfilePictureUrl = "pic.jpg",
                IsVerifiedCoach = true,
                Email = "john@example.com",
                UserName = "john.doe"
            };

            await using var db = CreateDb(Guid.NewGuid().ToString());
            db.ApplicationUsers.Add(user);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            // Act
            var vm = await service.GetUserProfileAsync(user.Id);

            // Assert
            Assert.That(vm, Is.Not.Null);
            Assert.That(vm!.UserId, Is.EqualTo(user.Id));
            Assert.That(vm.FullName, Is.EqualTo("John Doe"));
            Assert.That(vm.ProfilePictureUrl, Is.EqualTo("pic.jpg"));
            Assert.That(vm.IsVerifiedCoach, Is.True);
            Assert.That(vm.Friends, Is.Not.Null);
            Assert.That(vm.ActiveTab, Is.EqualTo("Overview"));
        }

        [Test]
        public async Task UpdateAboutTabAsync_InvalidInputs_ThrowsArgumentException()
        {
            // Arrange
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var service = CreateService(db);

            // Act + Assert: user not found
            Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAboutTabAsync(Guid.NewGuid(), new AboutTabViewModel()));
        }

        [Test]
        public async Task UpdateAboutTabAsync_WritesJsonFile_WithProvidedModel()
        {
            // Arrange
            var (baseDir, cleanup) = UseIsolatedWorkingDir();
            try
            {
                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    FirstName = "Jane",
                    LastName = "Roe",
                    ProfilePictureUrl = "p.jpg",
                    Email = "jane@example.com",
                    UserName = "jane.roe"
                };

                await using var db = CreateDb(Guid.NewGuid().ToString());
                db.ApplicationUsers.Add(user);
                await db.SaveChangesAsync();
                var service = CreateService(db);

                var about = new AboutTabViewModel
                {
                    UserId = user.Id,
                    BackgroundImageUrl = "https://dropbox/bg.png",
                    PersonalInfo = new PersonalInfoSection { About = "Hello" },
                    ContactInfo = new ContactInfoSection { Email = "jane@example.com" },
                    RelationshipStatus = "Single",
                    Gender = "F"
                };

                // Act
                await service.UpdateAboutTabAsync(user.Id, about);

                // Assert: verify file exists and contains JSON
                var path = Path.Combine("UserData", "AboutTabs", $"about_{user.Id}.json");
                Assert.That(File.Exists(path), Is.True, $"Expected about file at {path}");
                var content = await File.ReadAllTextAsync(path);
                var roundtrip = JsonSerializer.Deserialize<AboutTabViewModel>(content);
                Assert.That(roundtrip, Is.Not.Null);
                Assert.That(roundtrip!.BackgroundImageUrl, Is.EqualTo("https://dropbox/bg.png"));
                Assert.That(roundtrip.PersonalInfo.About, Is.EqualTo("Hello"));
            }
            finally
            {
                cleanup();
            }
        }

        [Test]
        public async Task GetProfileDataAsync_NoFile_ReturnsDefaultObject()
        {
            // Arrange
            var (baseDir, cleanup) = UseIsolatedWorkingDir();
            try
            {
                await using var db = CreateDb(Guid.NewGuid().ToString());
                var service = CreateService(db);

                // Act
                var data = await service.GetProfileDataAsync(Guid.NewGuid());

                // Assert
                Assert.That(data, Is.Not.Null);
                Assert.That(data!.IsEmpty, Is.True);
            }
            finally
            {
                cleanup();
            }
        }

        [Test]
        public async Task GetProfileDataAsync_FileExists_Deserializes()
        {
            // Arrange
            var (baseDir, cleanup) = UseIsolatedWorkingDir();
            try
            {
                var userId = Guid.NewGuid();
                var targetDir = Path.Combine("UserData", "ProfileData");
                Directory.CreateDirectory(targetDir);
                var expected = new UserProfileData { Headline = "Hi", Location = "Varna", AboutText = "About me" };
                var path = Path.Combine(targetDir, $"profile_{userId}.json");
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(expected));

                await using var db = CreateDb(Guid.NewGuid().ToString());
                var service = CreateService(db);

                // Act
                var data = await service.GetProfileDataAsync(userId);

                // Assert
                Assert.That(data, Is.Not.Null);
                Assert.That(data!.Headline, Is.EqualTo("Hi"));
                Assert.That(data.Location, Is.EqualTo("Varna"));
                Assert.That(data.AboutText, Is.EqualTo("About me"));
            }
            finally
            {
                cleanup();
            }
        }

        [Test]
        public async Task UpdateProfileDataAsync_WritesJson_WithAppliedChanges()
        {
            // Arrange
            var (baseDir, cleanup) = UseIsolatedWorkingDir();
            try
            {
                var userId = Guid.NewGuid();
                await using var db = CreateDb(Guid.NewGuid().ToString());
                var service = CreateService(db);

                // Act
                await service.UpdateProfileDataAsync(userId, d =>
                {
                    d.Headline = "Coach";
                    d.Location = "Sofia";
                });

                // Assert
                var path = Path.Combine("UserData", "ProfileData", $"profile_{userId}.json");
                Assert.That(File.Exists(path), Is.True);
                var roundtrip = JsonSerializer.Deserialize<UserProfileData>(await File.ReadAllTextAsync(path));
                Assert.That(roundtrip, Is.Not.Null);
                Assert.That(roundtrip!.Headline, Is.EqualTo("Coach"));
                Assert.That(roundtrip.Location, Is.EqualTo("Sofia"));
            }
            finally
            {
                cleanup();
            }
        }

        [Test]
        public async Task GetFriendsTabAsync_UserNotFound_ReturnsNull()
        {
            // Arrange
            await using var db = CreateDb(Guid.NewGuid().ToString());
            var service = CreateService(db);

            // Act
            var result = await service.GetFriendsTabAsync(Guid.NewGuid());

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetFriendsTabAsync_UserFound_ReturnsEmptyCollections()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(), FirstName = "A", LastName = "B", ProfilePictureUrl = "u.jpg", Email = "a@b.com", UserName = "ab"
            };
            await using var db = CreateDb(Guid.NewGuid().ToString());
            db.ApplicationUsers.Add(user);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            // Act
            var friends = await service.GetFriendsTabAsync(user.Id);

            // Assert
            Assert.That(friends, Is.Not.Null);
            Assert.That(friends!.TotalFriendsCount, Is.EqualTo(0));
            Assert.That(friends.Friends, Is.Not.Null);
        }

        [Test]
        public void Constructor_NullDependencies_Throws()
        {
            // Arrange
            var db = CreateDb(Guid.NewGuid().ToString());

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => new UserProfileService(null!, Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(), db));
            Assert.Throws<ArgumentNullException>(() => new UserProfileService(Mock.Of<IFileSystem>(), null!, db));
            Assert.Throws<ArgumentNullException>(() => new UserProfileService(Mock.Of<IFileSystem>(), Mock.Of<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(), null!));
        }
    }
}
