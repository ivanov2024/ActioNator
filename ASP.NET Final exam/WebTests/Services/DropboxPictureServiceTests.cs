using System.Text;
using ActioNator.Services.ContentInspectors;
using ActioNator.Services.Implementations.Cloud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class DropboxPictureServiceTests
    {
        private static IFormFile MakeFile(byte[] content, string contentType, string fileName)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static DropboxPictureService CreateService(ILogger<DropboxPictureService>? logger = null)
        {
            var inspectorLogger = new Mock<ILogger<ImageContentInspector>>();
            var inspector = new ImageContentInspector(inspectorLogger.Object);
            var svcLogger = logger ?? new Mock<ILogger<DropboxPictureService>>().Object;
            return new DropboxPictureService(svcLogger, inspector);
        }

        [Test]
        public void UploadUserProfilePictureAsync_Throws_OnPdfByMime()
        {
            var service = CreateService();
            var file = MakeFile(Encoding.UTF8.GetBytes("fake pdf"), "application/pdf", "doc.pdf");
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.InvalidImageFormatException>(async () =>
                await service.UploadUserProfilePictureAsync(file, "user1", "token", CancellationToken.None));
        }

        [Test]
        public void UploadUserProfilePictureAsync_Throws_OnPdfByExtension()
        {
            var service = CreateService();
            var file = MakeFile(Encoding.UTF8.GetBytes("fake"), "image/jpeg", "doc.pdf");
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.InvalidImageFormatException>(async () =>
                await service.UploadUserProfilePictureAsync(file, "user1", "token", CancellationToken.None));
        }

        [Test]
        public void UploadUserProfilePictureAsync_Throws_OnUnsupportedMime()
        {
            var service = CreateService();
            var file = MakeFile(Encoding.UTF8.GetBytes("fake"), "text/plain", "note.txt");
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.InvalidImageFormatException>(async () =>
                await service.UploadUserProfilePictureAsync(file, "user1", "token", CancellationToken.None));
        }

        [Test]
        public void UploadUserProfilePictureAsync_Throws_OnMimeExtMismatch()
        {
            var service = CreateService();
            // JPEG mime but .png extension -> mismatch
            var file = MakeFile(Encoding.UTF8.GetBytes("fake"), "image/jpeg", "pic.png");
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.InvalidImageFormatException>(async () =>
                await service.UploadUserProfilePictureAsync(file, "user1", "token", CancellationToken.None));
        }

        [Test]
        public void UploadUserProfilePictureAsync_Throws_WhenInspectorRejectsContent()
        {
            var service = CreateService();
            // Content type image/jpeg, but bytes are not JPEG signatures; inspector returns false
            var file = MakeFile(Encoding.UTF8.GetBytes("not_a_jpeg"), "image/jpeg", "pic.jpg");
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.InvalidImageFormatException>(async () =>
                await service.UploadUserProfilePictureAsync(file, "user1", "token", CancellationToken.None));
        }

        [Test]
        public void DeleteUserProfilePictureAsync_Throws_OnEmptyPath()
        {
            var service = CreateService();
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.DeleteUserProfilePictureAsync(string.Empty, "token", CancellationToken.None));
        }

        [Test]
        public void DeleteUserProfilePictureAsync_Throws_OnEmptyToken()
        {
            var service = CreateService();
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await service.DeleteUserProfilePictureAsync("/users/u/pic.jpg", string.Empty, CancellationToken.None));
        }
    }
}
