using System.Text.RegularExpressions;
using System.Linq;
using ActioNator.Services.Configuration;
using ActioNator.Services.Implementations.FileServices;
using ActioNator.Services.Interfaces.FileServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class FileStorageServiceTests
    {
        private Mock<IWebHostEnvironment> _envMock = null!;
        private Mock<IFileSystem> _fsMock = null!;
        private Mock<ILogger<FileStorageService>> _loggerMock = null!;
        private FileUploadOptions _options = null!;
        private FileStorageService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.SetupGet(e => e.ContentRootPath).Returns("C:\\app");

            _fsMock = new Mock<IFileSystem>(MockBehavior.Strict);
            _fsMock
                .Setup(m => m.CombinePaths(It.IsAny<string[]>()))
                .Returns((string[] parts) => System.IO.Path.Combine(parts));
            _fsMock
                .Setup(m => m.CreateDirectory(It.IsAny<string>()))
                .Verifiable();

            _loggerMock = new Mock<ILogger<FileStorageService>>();
            _options = new FileUploadOptions();

            _service = new FileStorageService(
                _envMock.Object,
                Options.Create(_options),
                _loggerMock.Object,
                _fsMock.Object
            );
        }

        private static IFormFile CreateFormFile(string fileName)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.SetupGet(f => f.FileName).Returns(fileName);
            fileMock
                .Setup(f => f.CopyToAsync(It.IsAny<System.IO.Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns<System.IO.Stream, System.Threading.CancellationToken>((_, __) => Task.CompletedTask);
            return fileMock.Object;
        }

        [Test]
        public async Task SaveFilesAsync_SavesToUserFolder_AndReturnsRelativePaths()
        {
            var files = new FormFileCollection
            {
                CreateFormFile("report.pdf"),
                CreateFormFile("photo.JPG")
            };

            var createdPaths = new List<string>();
            _fsMock
                .Setup(m => m.Create(It.IsAny<string>()))
                .Returns<string>(p => { createdPaths.Add(p); return new System.IO.MemoryStream(); });

            var result = await _service.SaveFilesAsync(files, "uploads", "user123");
            var resultList = result.ToList();

            Assert.That(resultList, Has.Count.EqualTo(2));
            Assert.That(resultList.All(p => p.StartsWith($"uploads{System.IO.Path.DirectorySeparatorChar}user123")), Is.True);
            Assert.That(resultList.Any(p => p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(resultList.Any(p => p.EndsWith(".JPG", StringComparison.OrdinalIgnoreCase)), Is.True);

            // Ensure directory creation and file creation happened
            _fsMock.Verify(m => m.CreateDirectory(System.IO.Path.Combine("C:\\app", "uploads", "user123")), Times.Once);
            Assert.That(createdPaths.Count, Is.EqualTo(2));
        }

        [Test]
        public void SaveFilesAsync_Throws_OnInvalidArguments()
        {
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.FileStorageException>(
                () => _service.SaveFilesAsync(null!, "uploads", "user123")
            );

            var empty = new FormFileCollection();
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.FileStorageException>(
                () => _service.SaveFilesAsync(empty, "uploads", "user123")
            );

            var one = new FormFileCollection { CreateFormFile("a.txt") };
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.FileStorageException>(
                () => _service.SaveFilesAsync(one, null!, "user123")
            );
            Assert.ThrowsAsync<ActioNator.Services.Exceptions.FileStorageException>(
                () => _service.SaveFilesAsync(one, "uploads", null!)
            );
        }

        [Test]
        public async Task GetFileAsync_ReturnsStreamAndContentType_WhenAuthorizedAndExists()
        {
            // Arrange
            string userId = "user123";
            string relPath = $"uploads{System.IO.Path.DirectorySeparatorChar}{userId}{System.IO.Path.DirectorySeparatorChar}file.pdf";
            string fullPath = System.IO.Path.Combine("C:\\app", relPath);

            _fsMock
                .Setup(m => m.FileExists(fullPath))
                .Returns(true);
            _fsMock
                .Setup(m => m.GetExtension(fullPath))
                .Returns(".pdf");
            _fsMock
                .Setup(m => m.OpenRead(fullPath))
                .Returns(new System.IO.MemoryStream(new byte[] { 1, 2, 3 }));

            // Act
            var (stream, contentType) = await _service.GetFileAsync(relPath, userId);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(contentType, Is.EqualTo("application/pdf"));
        }

        [Test]
        public void GetFileAsync_ThrowsUnauthorizedAccess_WhenUserNotAuthorized()
        {
            string userId = "user123";
            // Missing the required \userId\ segment
            string relPath = $"uploads{System.IO.Path.DirectorySeparatorChar}other{System.IO.Path.DirectorySeparatorChar}file.pdf";

            Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.GetFileAsync(relPath, userId)
            );
        }

        [Test]
        public void GetFileAsync_ThrowsFileNotFound_WhenMissing()
        {
            string userId = "user123";
            string relPath = $"uploads{System.IO.Path.DirectorySeparatorChar}{userId}{System.IO.Path.DirectorySeparatorChar}missing.pdf";
            string fullPath = System.IO.Path.Combine("C:\\app", relPath);

            _fsMock
                .Setup(m => m.FileExists(fullPath))
                .Returns(false);

            Assert.ThrowsAsync<FileNotFoundException>(
                () => _service.GetFileAsync(relPath, userId)
            );
        }

        [Test]
        public void GetSafeFileName_SanitizesAndFormats()
        {
            string input = "inva|id..\\name<script>.png";
            string output = _service.GetSafeFileName(input);

            Assert.That(output, Does.EndWith(".png"));
            // Should not contain invalid characters or traversal patterns
            Assert.That(output.Contains("|"), Is.False);
            Assert.That(output.Contains("..\\"), Is.False);
            // Matches: base_yyyyMMddHHmmss_XXXXXXXX.ext
            var re = new Regex(@"^[A-Za-z0-9_.-]+_\d{14}_[0-9a-fA-F]{8}\.png$");
            Assert.That(re.IsMatch(output), Is.True, $"Unexpected format: {output}");
        }

        [Test]
        public async Task IsUserAuthorizedForFileAsync_True_WhenUserFolderPresent()
        {
            string userId = "userABC";
            string relPath = $"root{System.IO.Path.DirectorySeparatorChar}{userId}{System.IO.Path.DirectorySeparatorChar}f.txt";
            bool ok = await _service.IsUserAuthorizedForFileAsync(relPath, userId);
            Assert.That(ok, Is.True);
        }

        [Test]
        public async Task IsUserAuthorizedForFileAsync_False_WhenWrongSeparators()
        {
            string userId = "userABC";
            // Use forward slashes intentionally to illustrate mismatch on Windows-style separator check
            string relPath = $"root/{userId}/f.txt";
            bool ok = await _service.IsUserAuthorizedForFileAsync(relPath, userId);
            Assert.That(ok, Is.False);
        }
    }
}
