using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    [TestFixture]
    public class FileValidationOrchestratorTests
    {
        private Mock<IFileValidatorFactory> _factoryMock = null!;
        private Mock<IFileValidator> _imageValidatorMock = null!;
        private Mock<IFileValidator> _pdfValidatorMock = null!;
        private Mock<ILogger<FileValidationOrchestrator>> _loggerMock = null!;
        private FileValidationOrchestrator _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _factoryMock = new Mock<IFileValidatorFactory>(MockBehavior.Strict);
            _imageValidatorMock = new Mock<IFileValidator>(MockBehavior.Strict);
            _pdfValidatorMock = new Mock<IFileValidator>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<FileValidationOrchestrator>>(MockBehavior.Loose);

            _sut = new FileValidationOrchestrator(_factoryMock.Object, _loggerMock.Object);
        }

        private static IFormFile MakeFile(string name, string contentType, int size = 10)
        {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', size)));
            var file = new FormFile(ms, 0, ms.Length, name, name)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
            return file;
        }

        [Test]
        public async Task ValidateFilesAsync_EmptyFiles_ReturnsFailure()
        {
            var files = new FormFileCollection();
            var result = await _sut.ValidateFilesAsync(files);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public async Task ValidateFilesAsync_AllValidSameType_ReturnsSuccess()
        {
            var files = new FormFileCollection
            {
                MakeFile("a.png", "image/png"),
                MakeFile("b.png", "image/png")
            };

            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("image/png")).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Success());

            var result = await _sut.ValidateFilesAsync(files);
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public async Task ValidateFilesAsync_MixedTypes_ReturnsFailureWithMetadata()
        {
            var files = new FormFileCollection
            {
                MakeFile("a.png", "image/png"),
                MakeFile("c.pdf", "application/pdf")
            };

            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("image/png")).Returns(true);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("application/pdf")).Returns(false);

            var result = await _sut.ValidateFilesAsync(files);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ValidationMetadata, Is.Not.Null);
            var meta = result.ValidationMetadata as Dictionary<string, object>;
            Assert.That(meta, Is.Not.Null);
            Assert.That(meta!.ContainsKey("ExpectedType"), Is.True);
            Assert.That(meta!.ContainsKey("FoundType"), Is.True);
        }

        [Test]
        public async Task ValidateFilesAsync_ValidatorFailsForAFile_PropagatesFailureWithFileMetadata()
        {
            var files = new FormFileCollection
            {
                MakeFile("a.png", "image/png"),
                MakeFile("b.png", "image/png")
            };

            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("image/png")).Returns(true);
            _imageValidatorMock
                .SetupSequence(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Success())
                .ReturnsAsync(FileValidationResult.Failure("too big"));

            var result = await _sut.ValidateFilesAsync(files);
            Assert.That(result.IsValid, Is.False);
            var meta = result.ValidationMetadata as Dictionary<string, object>;
            Assert.That(meta, Is.Not.Null);
            Assert.That(meta!.ContainsKey("FileName"), Is.True);
            Assert.That(meta!.ContainsKey("FileSize"), Is.True);
        }

        [Test]
        public async Task ValidateFilesAsync_UnsupportedType_ReturnsFailure()
        {
            var files = new FormFileCollection { MakeFile("a.txt", "text/plain") };
            _factoryMock.Setup(f => f.GetValidatorForContentType("text/plain")).Throws(new InvalidOperationException("unsupported"));

            var result = await _sut.ValidateFilesAsync(files);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Unsupported file type"));
        }

        [Test]
        public void ValidateFilesAsync_Canceled_ThrowsOperationCanceled()
        {
            var files = new FormFileCollection
            {
                MakeFile("a.png", "image/png"),
                MakeFile("b.png", "image/png")
            };

            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("image/png")).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Success());

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(async () => await _sut.ValidateFilesAsync(files, cts.Token));
        }

        [Test]
        public async Task ValidateFilesAsync_ValidatorThrowsFileValidationException_ReturnsFailure()
        {
            var files = new FormFileCollection { MakeFile("a.png", "image/png") };

            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("image/png")).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FileValidationException("bad"));

            var result = await _sut.ValidateFilesAsync(files);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("bad"));
        }

        [Test]
        public void ValidateFilesAsync_UnexpectedException_WrapsInFileValidationException()
        {
            var files = new FormFileCollection { MakeFile("a.png", "image/png") };

            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType("image/png")).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            Assert.ThrowsAsync<FileValidationException>(async () => await _sut.ValidateFilesAsync(files));
        }

        [Test]
        public async Task ValidateFileAsync_Null_ReturnsFailure()
        {
            var result = await _sut.ValidateFileAsync(null!);
            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public async Task ValidateFileAsync_SupportedType_Success()
        {
            var file = MakeFile("a.pdf", "application/pdf");
            _factoryMock.Setup(f => f.GetValidatorForContentType("application/pdf")).Returns(_pdfValidatorMock.Object);
            _pdfValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Success());
            _pdfValidatorMock.Setup(v => v.CanHandleFileType(It.IsAny<string>())).Returns(true);

            var result = await _sut.ValidateFileAsync(file);
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public async Task ValidateFileAsync_UnsupportedType_ReturnsFailure()
        {
            var file = MakeFile("a.txt", "text/plain");
            _factoryMock.Setup(f => f.GetValidatorForContentType("text/plain")).Throws(new InvalidOperationException());

            var result = await _sut.ValidateFileAsync(file);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Unsupported file type"));
        }

        [Test]
        public async Task ValidateFileAsync_ValidatorFailure_ReturnsFailureWithMetadata()
        {
            var file = MakeFile("a.png", "image/png");
            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(FileValidationResult.Failure("bad"));
            _imageValidatorMock.Setup(v => v.CanHandleFileType(It.IsAny<string>())).Returns(true);

            var result = await _sut.ValidateFileAsync(file);
            Assert.That(result.IsValid, Is.False);
            var meta = result.ValidationMetadata as Dictionary<string, object>;
            Assert.That(meta, Is.Not.Null);
            Assert.That(meta!.ContainsKey("FileName"), Is.True);
            Assert.That(meta!.ContainsKey("FileSize"), Is.True);
        }

        [Test]
        public void ValidateFileAsync_Canceled_ThrowsOperationCanceled()
        {
            var file = MakeFile("a.png", "image/png");
            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType(It.IsAny<string>())).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .Callback<IFormFile, CancellationToken>((_, ct) => ct.ThrowIfCancellationRequested())
                .ReturnsAsync(FileValidationResult.Success());

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(async () => await _sut.ValidateFileAsync(file, cts.Token));
        }

        [Test]
        public async Task ValidateFileAsync_ValidatorThrowsFileValidationException_ReturnsFailure()
        {
            var file = MakeFile("a.png", "image/png");
            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType(It.IsAny<string>())).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new FileValidationException("problem"));

            var result = await _sut.ValidateFileAsync(file);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("problem"));
        }

        [Test]
        public void ValidateFileAsync_UnexpectedException_WrapsInFileValidationException()
        {
            var file = MakeFile("a.png", "image/png");
            _factoryMock.Setup(f => f.GetValidatorForContentType("image/png")).Returns(_imageValidatorMock.Object);
            _imageValidatorMock.Setup(v => v.CanHandleFileType(It.IsAny<string>())).Returns(true);
            _imageValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("boom"));

            Assert.ThrowsAsync<FileValidationException>(async () => await _sut.ValidateFileAsync(file));
        }

        [Test]
        public void AreAllFilesSameType_VariousCases()
        {
            Assert.That(_sut.AreAllFilesSameType(null!), Is.True);
            Assert.That(_sut.AreAllFilesSameType(new FormFileCollection()), Is.True);
            Assert.That(_sut.AreAllFilesSameType(new FormFileCollection { MakeFile("a.png", "image/png") }), Is.True);

            var imgs = new FormFileCollection { MakeFile("a.png", "image/png"), MakeFile("b.jpg", "image/jpeg") };
            Assert.That(_sut.AreAllFilesSameType(imgs), Is.True);

            var pdfs = new FormFileCollection { MakeFile("a.pdf", "application/pdf"), MakeFile("b.pdf", "application/pdf") };
            Assert.That(_sut.AreAllFilesSameType(pdfs), Is.True);

            var mixed = new FormFileCollection { MakeFile("a.pdf", "application/pdf"), MakeFile("b.png", "image/png") };
            Assert.That(_sut.AreAllFilesSameType(mixed), Is.False);
        }
    }
}
