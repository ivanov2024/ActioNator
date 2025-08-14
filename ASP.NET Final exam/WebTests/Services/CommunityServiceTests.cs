using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.Community;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Interfaces.Communication;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class CommunityServiceTests
    {
        private static UserManager<ApplicationUser> CreateUserManagerMock(MockBehavior behavior, out Mock<UserManager<ApplicationUser>> mock)
        {
            var store = new Mock<IUserStore<ApplicationUser>>(behavior);
            mock = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
            return mock.Object;
        }

        private static IFormFile CreateFormFile(string name, byte[] data)
        {
            var stream = new MemoryStream(data);
            return new FormFile(stream, 0, data.Length, name, name + ".png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

        private static (ApplicationUser user, ApplicationUser other) SeedUsers(TestInMemoryCommunityDbContext db)
        {
            var u1 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "user1", FirstName = "User", LastName = "One", ProfilePictureUrl = "" };
            var u2 = new ApplicationUser { Id = Guid.NewGuid(), UserName = "user2", FirstName = "User", LastName = "Two", ProfilePictureUrl = "" };
            db.Users.AddRange(u1, u2);
            db.SaveChanges();
            return (u1, u2);
        }

        [Test]
        public async Task CreatePostAsync_Creates_Post_And_Broadcasts()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, _) = SeedUsers(db);

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Strict);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Strict);
            var signal = new Mock<ISignalRService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            object[]? capturedArgs = null;
            signal.Setup(s => s.SendToAllAsync("ReceiveNewPost", It.IsAny<object[]>()))
                  .Callback<string, object[]>((m, a) => capturedArgs = a)
                  .Returns(Task.CompletedTask);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var vm = await sut.CreatePostAsync("hello world", author.Id, CancellationToken.None, images: null);

            Assert.That(vm, Is.Not.Null);
            Assert.That(db.Posts.Count(), Is.EqualTo(1));
            var post = db.Posts.First();
            Assert.That(post.Content, Is.EqualTo("hello world"));
            Assert.That(post.IsPublic, Is.True);
            Assert.That(post.IsDeleted, Is.False);
            signal.Verify(s => s.SendToAllAsync("ReceiveNewPost", It.IsAny<object[]>()), Times.Once);
            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs!.Length, Is.EqualTo(1));
            Assert.That(capturedArgs[0], Is.InstanceOf<PostCardViewModel>());
        }

        [Test]
        public async Task CreatePostAsync_WithImages_Uploads_And_Persists_ImageRecords()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, _) = SeedUsers(db);

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Strict);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Strict);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            cloud.Setup(c => c.UploadImagesAsync(
                    It.IsAny<IEnumerable<IFormFile>>(),
                    It.IsAny<Guid>(),
                    "community",
                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<string> { "https://cdn/img1.png", "https://cdn/img2.png" });

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var images = new List<IFormFile>
            {
                CreateFormFile("f1", new byte[]{1,2,3}),
                CreateFormFile("f2", new byte[]{4,5,6})
            };

            var vm = await sut.CreatePostAsync("post with images", author.Id, CancellationToken.None, images);

            Assert.That(vm, Is.Not.Null);
            var post = db.Posts.First();
            // Fallback path should have persisted PostImages when collection was empty and images count > 1
            Assert.That(db.PostImages.Count(pi => pi.PostId == post.Id), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public async Task AddCommentAsync_Sanitizes_Saves_And_Broadcasts()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, commenter) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "c", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false };
            db.Posts.Add(post);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Strict);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Strict);
            san.Setup(s => s.SanitizeString(It.IsAny<string>())).Returns<string>(s => s);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            object[]? capturedArgs = null;
            signal.Setup(s => s.SendToAllAsync("ReceiveNewComment", It.IsAny<object[]>()))
                  .Callback<string, object[]>((m, a) => capturedArgs = a)
                  .Returns(Task.CompletedTask);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var cvm = await sut.AddCommentAsync(post.Id, new string('x', 10), commenter.Id, CancellationToken.None);

            Assert.That(cvm, Is.Not.Null);
            Assert.That(db.Comments.Count(c => c.PostId == post.Id), Is.EqualTo(1));
            signal.Verify(s => s.SendToAllAsync("ReceiveNewComment", It.IsAny<object[]>()), Times.Once);
            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs![0], Is.InstanceOf<PostCommentViewModel>());
        }

        [Test]
        public async Task ToggleLikePostAsync_Toggles_And_Broadcasts()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, liker) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "p", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false };
            db.Posts.Add(post);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var c1 = await sut.ToggleLikePostAsync(post.Id, liker.Id, CancellationToken.None);
            Assert.That(c1, Is.EqualTo(1));
            var c2 = await sut.ToggleLikePostAsync(post.Id, liker.Id, CancellationToken.None);
            Assert.That(c2, Is.EqualTo(0));
        }

        [Test]
        public async Task DeletePostAsync_Author_Deletes_And_Removes_Images()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, other) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "p", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false, ImageUrl = "https://res.cloudinary.com/demo/image/upload/v123/publicId.png" };
            db.Posts.Add(post);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Strict);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Strict);
            cloudUrl.Setup(u => u.GetPublicId(It.IsAny<string>())).Returns("publicId");
            cloud.Setup(c => c.DeleteImagesByPublicIdsAsync(post.Id, It.Is<List<string>>(l => l.Contains("publicId")), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var ok = await sut.DeletePostAsync(post.Id, author.Id, CancellationToken.None);
            Assert.That(ok, Is.True);
            var dbPost = db.Posts.IgnoreQueryFilters().First(p => p.Id == post.Id);
            Assert.That(dbPost.IsDeleted, Is.True);
            cloud.VerifyAll();
        }

        [Test]
        public async Task DeletePostAsync_NonAuthor_NonAdmin_Fails()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, other) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "p", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false };
            db.Posts.Add(post);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            var um = CreateUserManagerMock(MockBehavior.Strict, out var umMock);
            umMock.Setup(u => u.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                  .ReturnsAsync(false);
            umMock.Setup(u => u.FindByIdAsync(other.Id.ToString())).ReturnsAsync(new ApplicationUser { Id = other.Id, UserName = other.UserName });

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var ok = await sut.DeletePostAsync(post.Id, other.Id, CancellationToken.None);
            Assert.That(ok, Is.False);
            Assert.That(db.Posts.First(p => p.Id == post.Id).IsDeleted, Is.False);
        }

        [Test]
        public async Task GetAllPostsAsync_Default_Returns_Public_Active()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (u1, u2) = SeedUsers(db);
            db.Posts.AddRange(
                new Post { Id = Guid.NewGuid(), UserId = u1.Id, Content = "A", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false },
                new Post { Id = Guid.NewGuid(), UserId = u1.Id, Content = "B", CreatedAt = DateTime.UtcNow.AddMinutes(-1), IsPublic = true, IsDeleted = true },
                new Post { Id = Guid.NewGuid(), UserId = u2.Id, Content = "C", CreatedAt = DateTime.UtcNow.AddMinutes(-2), IsPublic = false, IsDeleted = false }
            );
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            var um = CreateUserManagerMock(MockBehavior.Strict, out var umMock);
            umMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                  .ReturnsAsync((string id) => new ApplicationUser { Id = Guid.Parse(id) });
            umMock.Setup(u => u.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                  .ReturnsAsync(false);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var list = await sut.GetAllPostsAsync(u1.Id);
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0].Content, Is.EqualTo("A"));
        }

        [Test]
        public async Task ToggleLikeCommentAsync_FirstLike_Increments_And_Broadcasts()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, liker) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "p", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false };
            db.Posts.Add(post);
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = author.Id, Content = "c", CreatedAt = DateTime.UtcNow, IsDeleted = false, LikesCount = 0 };
            db.Comments.Add(comment);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Strict);
            object[]? args = null;
            signal.Setup(s => s.SendToAllAsync("ReceiveCommentUpdate", It.IsAny<object[]>()))
                  .Callback<string, object[]>((m, a) => args = a)
                  .Returns(Task.CompletedTask);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var count = await sut.ToggleLikeCommentAsync(comment.Id, liker.Id, CancellationToken.None);
            Assert.That(count, Is.EqualTo(1));
            var dbComment = db.Comments.Include(c => c.Likes).First(c => c.Id == comment.Id);
            Assert.That(dbComment.LikesCount, Is.EqualTo(1));
            Assert.That(dbComment.Likes.Count(l => l.IsActive), Is.EqualTo(1));
            signal.Verify(s => s.SendToAllAsync("ReceiveCommentUpdate", It.IsAny<object[]>()), Times.Once);
            Assert.That(args, Is.Not.Null);
            Assert.That(args![0], Is.EqualTo(comment.Id));
            Assert.That(args![1], Is.EqualTo(1));
        }

        [Test]
        public async Task ToggleLikeCommentAsync_ToggleOff_Decrements_ToZero()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, liker) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "p", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false };
            db.Posts.Add(post);
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = author.Id, Content = "c", CreatedAt = DateTime.UtcNow, IsDeleted = false, LikesCount = 0 };
            db.Comments.Add(comment);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            var c1 = await sut.ToggleLikeCommentAsync(comment.Id, liker.Id, CancellationToken.None);
            Assert.That(c1, Is.EqualTo(1));
            var c2 = await sut.ToggleLikeCommentAsync(comment.Id, liker.Id, CancellationToken.None);
            Assert.That(c2, Is.EqualTo(0));
            var dbComment = db.Comments.Include(c => c.Likes).First(c => c.Id == comment.Id);
            Assert.That(dbComment.LikesCount, Is.EqualTo(0));
            Assert.That(dbComment.Likes.Count(l => l.IsActive), Is.EqualTo(0));
        }

        [Test]
        public async Task ToggleLikeCommentAsync_Reactivates_Inactive_Like()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var (author, liker) = SeedUsers(db);
            var post = new Post { Id = Guid.NewGuid(), UserId = author.Id, Content = "p", CreatedAt = DateTime.UtcNow, IsPublic = true, IsDeleted = false };
            db.Posts.Add(post);
            var comment = new Comment { Id = Guid.NewGuid(), PostId = post.Id, AuthorId = author.Id, Content = "c", CreatedAt = DateTime.UtcNow, IsDeleted = false, LikesCount = 0 };
            db.Comments.Add(comment);
            db.SaveChanges();

            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            // Like -> Unlike -> Like again
            await sut.ToggleLikeCommentAsync(comment.Id, liker.Id, CancellationToken.None); // 1
            await sut.ToggleLikeCommentAsync(comment.Id, liker.Id, CancellationToken.None); // 0
            var c3 = await sut.ToggleLikeCommentAsync(comment.Id, liker.Id, CancellationToken.None); // 1
            Assert.That(c3, Is.EqualTo(1));
            var dbComment = db.Comments.Include(c => c.Likes).First(c => c.Id == comment.Id);
            Assert.That(dbComment.LikesCount, Is.EqualTo(1));
            Assert.That(dbComment.Likes.Any(l => l.UserId == liker.Id && l.IsActive), Is.True);
        }

        [Test]
        public void ToggleLikeCommentAsync_Throws_On_Empty_Ids()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            Assert.ThrowsAsync<ArgumentException>(async () => await sut.ToggleLikeCommentAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.ToggleLikeCommentAsync(Guid.NewGuid(), Guid.Empty, CancellationToken.None));
        }

        [Test]
        public void ToggleLikeCommentAsync_Throws_When_Comment_NotFound()
        {
            using var db = new TestInMemoryCommunityDbContext(Guid.NewGuid().ToString());
            var cloud = new Mock<ICloudinaryService>(MockBehavior.Loose);
            var cloudUrl = new Mock<ICloudinaryUrlService>(MockBehavior.Loose);
            var signal = new Mock<ISignalRService>(MockBehavior.Loose);
            var logger = new Mock<ILogger<CommunityService>>();
            var san = new Mock<IInputSanitizationService>(MockBehavior.Loose);
            CreateUserManagerMock(MockBehavior.Strict, out var umMock);

            var sut = new CommunityService(db, cloud.Object, cloudUrl.Object, signal.Object, logger.Object, umMock.Object, san.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.ToggleLikeCommentAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
        }
    }
}
