using System.Text;
using System;
using System.IO;
using System.Linq;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.Cloud;
using ActioNator.Services.Interfaces.Cloud;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class CloudinaryServiceTests
    {
        private static TestInMemoryCommunityDbContext CreateDb(string name)
            => new TestInMemoryCommunityDbContext($"Cloudinary_{name}_{Guid.NewGuid()}");

        private static (ApplicationUser user, Guid userId) SeedUser(TestInMemoryCommunityDbContext db)
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                UserName = "testuser",
                FirstName = "Test",
                LastName = "User",
                ProfilePictureUrl = "https://example.com/p.png"
            };
            db.Users.Add(user);
            db.SaveChanges();
            return (user, user.Id);
        }

        private static Post SeedPostWithImages(TestInMemoryCommunityDbContext db, ApplicationUser user, Guid postId, params (string url, string publicId)[] images)
        {
            var post = new Post
            {
                Id = postId,
                UserId = user.Id,
                ApplicationUser = user,
                CreatedAt = DateTime.UtcNow,
                IsPublic = true
            };
            db.Posts.Add(post);

            foreach (var (url, _) in images)
            {
                db.PostImages.Add(new PostImage
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    ImageUrl = url
                });
            }

            db.SaveChanges();
            return post;
        }

        private static IFormFile CreateFormFile(string name, string contentType, byte[] data)
        {
            var stream = new MemoryStream(data);
            return new FormFile(stream, 0, data.Length, name, name + ".png")
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
        
        

        [Test]
        public async Task DeleteImagesByPublicIdsAsync_ReturnsTrue_WhenNoPublicIds()
        {
            using var db = CreateDb(nameof(DeleteImagesByPublicIdsAsync_ReturnsTrue_WhenNoPublicIds));
            var cloudinaryMock = new Mock<Cloudinary>(new Account("a","b","c"));
            var urlService = new Mock<ICloudinaryUrlService>();
            var logger = new Mock<ILogger<CloudinaryService>>();

            var service = new CloudinaryService(cloudinaryMock.Object, db, logger.Object, urlService.Object);
            var result = await service.DeleteImagesByPublicIdsAsync(Guid.NewGuid(), new List<string>(), CancellationToken.None);
            Assert.IsTrue(result);
        }

        

        [Test]
        public void UploadImagesAsync_Throws_WhenNoFiles()
        {
            using var db = CreateDb(nameof(UploadImagesAsync_Throws_WhenNoFiles));
            var cloudinaryMock = new Mock<Cloudinary>(new Account("a","b","c"));
            var urlService = new Mock<ICloudinaryUrlService>();
            var logger = new Mock<ILogger<CloudinaryService>>();
            var service = new CloudinaryService(cloudinaryMock.Object, db, logger.Object, urlService.Object);

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.UploadImagesAsync(Array.Empty<IFormFile>(), Guid.NewGuid(), "community", CancellationToken.None));
        }

        [Test]
        public void UploadImagesAsync_Throws_WhenAllFilesNull()
        {
            using var db = CreateDb(nameof(UploadImagesAsync_Throws_WhenAllFilesNull));
            var cloudinaryMock = new Mock<Cloudinary>(new Account("a","b","c"));
            var urlService = new Mock<ICloudinaryUrlService>();
            var logger = new Mock<ILogger<CloudinaryService>>();
            var service = new CloudinaryService(cloudinaryMock.Object, db, logger.Object, urlService.Object);

            var files = new IFormFile?[] { null, null }!
                .Where(f => f != null)!
                .Cast<IFormFile>();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.UploadImagesAsync(files, Guid.NewGuid(), "community", CancellationToken.None));
        }

        [Test]
        public async Task UploadImagesAsync_SingleFile_SetsPostImageUrl_And_NoPostImages()
        {
            using var db = CreateDb(nameof(UploadImagesAsync_SingleFile_SetsPostImageUrl_And_NoPostImages));
            var (user, _) = SeedUser(db);
            var postId = Guid.NewGuid();
            db.Posts.Add(new Post { Id = postId, UserId = user.Id, CreatedAt = DateTime.UtcNow, IsPublic = true });
            db.SaveChanges();

            var adapterMock = new Mock<ICloudinaryClientAdapter>();
            adapterMock
                .Setup(a => a.UploadAsync(It.IsAny<ImageUploadParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ImageUploadResult
                {
                    SecureUrl = new Uri("https://cdn.example.com/single.png")
                });

            var urlService = new Mock<ICloudinaryUrlService>();
            var logger = new Mock<ILogger<CloudinaryService>>();
            var service = new CloudinaryService(adapterMock.Object, db, logger.Object, urlService.Object);

            var file = CreateFormFile("img", "image/png", new byte[] { 1, 2, 3, 4 });
            var urls = await service.UploadImagesAsync(new[] { file }, postId, "community", CancellationToken.None);

            Assert.That(urls.Count(), Is.EqualTo(1));
            var post = db.Posts.First(p => p.Id == postId);
            Assert.That(post.ImageUrl, Is.EqualTo("https://cdn.example.com/single.png"));
            Assert.That(db.PostImages.Count(pi => pi.PostId == postId), Is.EqualTo(0));
        }

        [Test]
        public void UploadImagesAsync_UnsupportedContentType_Throws()
        {
            using var db = CreateDb(nameof(UploadImagesAsync_UnsupportedContentType_Throws));
            var (user, _) = SeedUser(db);
            var postId = Guid.NewGuid();
            db.Posts.Add(new Post { Id = postId, UserId = user.Id, CreatedAt = DateTime.UtcNow, IsPublic = true });
            db.SaveChanges();

            var cloudinaryMock = new Mock<Cloudinary>(new Account("a","b","c"));
            var urlService = new Mock<ICloudinaryUrlService>();
            var logger = new Mock<ILogger<CloudinaryService>>();
            var service = new CloudinaryService(cloudinaryMock.Object, db, logger.Object, urlService.Object);

            var badFile = CreateFormFile("bad", "text/plain", new byte[] { 1, 2, 3 });
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.UploadImagesAsync(new[] { badFile }, postId, "community", CancellationToken.None));
        }

        [Test]
        public async Task DeleteImagesByPublicIdsAsync_RemovesMatching_PostImages_WhenCloudinaryDeleted()
        {
            using var db = CreateDb(nameof(DeleteImagesByPublicIdsAsync_RemovesMatching_PostImages_WhenCloudinaryDeleted));
            var (user, _) = SeedUser(db);
            var postId = Guid.NewGuid();
            var post = new Post { Id = postId, UserId = user.Id, CreatedAt = DateTime.UtcNow, IsPublic = true };
            db.Posts.Add(post);
            db.PostImages.AddRange(
                new PostImage { Id = Guid.NewGuid(), PostId = postId, ImageUrl = "url1" },
                new PostImage { Id = Guid.NewGuid(), PostId = postId, ImageUrl = "url2" }
            );
            db.SaveChanges();

            var adapterMock = new Mock<ICloudinaryClientAdapter>();
            adapterMock
                .Setup(a => a.DeleteResourcesAsync(It.IsAny<DelResParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DelResResult
                {
                    Deleted = new Dictionary<string, string>
                    {
                        ["id1"] = "deleted",
                        ["id2"] = "deleted"
                    }
                });

            var urlService = new Mock<ICloudinaryUrlService>();
            urlService.Setup(u => u.GetPublicId("url1")).Returns("id1");
            urlService.Setup(u => u.GetPublicId("url2")).Returns("id2");

            var logger = new Mock<ILogger<CloudinaryService>>();
            var service = new CloudinaryService(adapterMock.Object, db, logger.Object, urlService.Object);

            var ok = await service.DeleteImagesByPublicIdsAsync(postId, new List<string> { "id1", "id2" }, CancellationToken.None);
            Assert.That(ok, Is.True);
            Assert.That(db.PostImages.Count(pi => pi.PostId == postId), Is.EqualTo(0));
        }
    }
}
