using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using ActioNator.Services.Implementations.VerifyCoach;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.Services.Models;

namespace WebTests.Services
{
    [TestFixture]
    public class CoachDocumentUploadServiceTests
    {
        private Mock<IFileValidationOrchestrator> _validationOrchestratorMock = null!;
        private Mock<IFileStorageService> _fileStorageMock = null!;
        private Mock<IFileSystem> _fileSystemMock = null!;
        private Mock<ILogger<CoachDocumentUploadService>> _loggerMock = null!;
        private IOptions<FileUploadOptions> _options = null!;
        private CoachDocumentUploadService _service = null!;

        [SetUp]
        public void SetUp()
        {
            _validationOrchestratorMock = new Mock<IFileValidationOrchestrator>(MockBehavior.Strict);
            _fileStorageMock = new Mock<IFileStorageService>(MockBehavior.Strict);
            _fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<CoachDocumentUploadService>>();

            _options = Options.Create(new FileUploadOptions
            {
                BasePath = "App_Data/coach-verifications",
                MaxFileSize = 10 * 1024 * 1024 // 10 MB
            });

            _service = new CoachDocumentUploadService(
                _validationOrchestratorMock.Object,
                _fileStorageMock.Object,
                _fileSystemMock.Object,
                _options,
                _loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _validationOrchestratorMock.VerifyAll();
            _fileStorageMock.VerifyAll();
            _fileSystemMock.VerifyAll();
        }

        [Test]
        public async Task ProcessUploadAsync_Success_ImageFiles_ReturnsSuccessWithSavedFiles()
        {
            // Arrange
            var files = new FormFileCollection
            {
                CreateFormFile("selfie.png", contentType: "image/png", size: 1024),
                CreateFormFile("certificate.jpg", contentType: "image/jpeg", size: 2048)
            };
            var userId = "user123";
            var uploadDir = _options.Value.BasePath; // service calls _fileSystem.CombinePaths(basePath)

            _fileSystemMock
                .Setup(fs => fs.CombinePaths(_options.Value.BasePath))
                .Returns(uploadDir);

            _validationOrchestratorMock
                .Setup(v => v.ValidateFilesAsync(files, It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Success());

            var savedPaths = new List<string>
            {
                $"{uploadDir}/{userId}/selfie_20250101000000_abcd.png",
                $"{uploadDir}/{userId}/certificate_20250101000000_ef01.jpg"
            };

            _fileStorageMock
                .Setup(s => s.SaveFilesAsync(files, uploadDir, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(savedPaths);

            _fileSystemMock
                .Setup(fs => fs.GetFileName(savedPaths[0]))
                .Returns("selfie_20250101000000_abcd.png");
            _fileSystemMock
                .Setup(fs => fs.GetFileName(savedPaths[1]))
                .Returns("certificate_20250101000000_ef01.jpg");

            // Act
            var result = await _service.ProcessUploadAsync(files, userId, CancellationToken.None);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Files.Count, Is.EqualTo(2));
            Assert.That(result.Files[0].OriginalFileName, Is.EqualTo("selfie.png"));
            Assert.That(result.Files[0].StoredFileName, Is.EqualTo("selfie_20250101000000_abcd.png"));
            Assert.That(result.Files[0].FilePath, Is.EqualTo(savedPaths[0]));
            Assert.That(result.Files[0].FileSize, Is.EqualTo(1024));
            Assert.That(result.Files[0].ContentType, Is.EqualTo("image/png"));
            Assert.That(result.Files[1].OriginalFileName, Is.EqualTo("certificate.jpg"));
        }

        [Test]
        public async Task ProcessUploadAsync_UserIdMissing_ReturnsFailure()
        {
            // Arrange
            var files = new FormFileCollection { CreateFormFile("doc.pdf", contentType: "application/pdf", size: 1) };

            // Act
            var result = await _service.ProcessUploadAsync(files, "", CancellationToken.None);

            // Assert
            Assert.That(result.Success, Is.False);
            StringAssert.Contains("User identification failed", result.Message);
        }

        [Test]
        public async Task ProcessUploadAsync_TotalSizeExceedsLimit_ReturnsFailureWithDetails()
        {
            // Arrange (set max 1 KB)
            _options = Options.Create(new FileUploadOptions
            {
                BasePath = "App_Data/coach-verifications",
                MaxFileSize = 1024
            });
            _service = new CoachDocumentUploadService(
                _validationOrchestratorMock.Object,
                _fileStorageMock.Object,
                _fileSystemMock.Object,
                _options,
                _loggerMock.Object);

            var files = new FormFileCollection
            {
                CreateFormFile("a.jpg", contentType: "image/jpeg", size: 900),
                CreateFormFile("b.jpg", contentType: "image/jpeg", size: 200)
            }; // total 1100 > 1024

            // Act
            var result = await _service.ProcessUploadAsync(files, "user123", CancellationToken.None);

            // Assert
            Assert.That(result.Success, Is.False);
            StringAssert.Contains("exceeds the maximum limit", result.Message);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.ErrorType, Is.EqualTo("FileSizeExceeded"));
            Assert.That(result.Error.AdditionalInfo.ContainsKey("TotalSize"), Is.True);
            Assert.That(result.Error.AdditionalInfo.ContainsKey("MaxSize"), Is.True);
        }

        [Test]
        public async Task ProcessUploadAsync_ValidationFailure_ReturnsFailureWithValidationDetails()
        {
            // Arrange
            var files = new FormFileCollection { CreateFormFile("c.txt", contentType: "text/plain", size: 50) };
            var metadata = new Dictionary<string, object> { { "Reason", "Unsupported type" } };

            _validationOrchestratorMock
                .Setup(v => v.ValidateFilesAsync(files, It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Failure("Unsupported file type", metadata));

            // Act
            var result = await _service.ProcessUploadAsync(files, "user123", CancellationToken.None);

            // Assert
            Assert.That(result.Success, Is.False);
            StringAssert.StartsWith("File validation failed: Unsupported file type", result.Message);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.ErrorType, Is.EqualTo("FileValidationFailed"));
            Assert.That(result.Error.AdditionalInfo.ContainsKey("ValidationMetadata"), Is.True);
        }

        [Test]
        public async Task ProcessUploadAsync_ValidationThrowsException_ReturnsFailure()
        {
            // Arrange
            var files = new FormFileCollection { CreateFormFile("a.png", contentType: "image/png", size: 10) };

            _validationOrchestratorMock
                .Setup(v => v.ValidateFilesAsync(files, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FileValidationException("Bad file"));

            // Act
            var result = await _service.ProcessUploadAsync(files, "user123", CancellationToken.None);

            // Assert
            Assert.That(result.Success, Is.False);
            StringAssert.StartsWith("File validation error:", result.Message);
        }

        [Test]
        public async Task ProcessUploadAsync_SaveFilesThrowsStorageException_ReturnsFailure()
        {
            // Arrange
            var files = new FormFileCollection { CreateFormFile("a.png", contentType: "image/png", size: 10) };
            var userId = "user123";
            var uploadDir = _options.Value.BasePath;

            _fileSystemMock
                .Setup(fs => fs.CombinePaths(_options.Value.BasePath))
                .Returns(uploadDir);

            _validationOrchestratorMock
                .Setup(v => v.ValidateFilesAsync(files, It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Success());

            _fileStorageMock
                .Setup(s => s.SaveFilesAsync(files, uploadDir, userId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FileStorageException("Disk error"));

            // Act
            var result = await _service.ProcessUploadAsync(files, userId, CancellationToken.None);

            // Assert
            Assert.That(result.Success, Is.False);
            StringAssert.StartsWith("File storage error:", result.Message);
        }

        [Test]
        public async Task ProcessUploadAsync_Cancellation_ReturnsFailureOperationCancelled()
        {
            // Arrange
            var files = new FormFileCollection { CreateFormFile("a.png", contentType: "image/png", size: 10) };

            _validationOrchestratorMock
                .Setup(v => v.ValidateFilesAsync(files, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            // Act
            var result = await _service.ProcessUploadAsync(files, "user123", new CancellationToken(canceled: true));

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error!.ErrorType, Is.EqualTo("OperationCancelled"));
            Assert.That(result.Message, Is.EqualTo("The operation was cancelled."));
        }

        private static IFormFile CreateFormFile(string fileName, string contentType = "application/octet-stream", int size = 128)
        {
            var bytes = Encoding.UTF8.GetBytes(new string('x', size));
            var stream = new System.IO.MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
    }
}
