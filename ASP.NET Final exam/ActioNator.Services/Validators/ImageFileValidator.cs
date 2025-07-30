using ActioNator.Services.ContentInspectors;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ActioNator.GCommon;
using ActioNator.Services.Interfaces.FileServices;

namespace ActioNator.Services.Validators
{
    /// <summary>
    /// Validator for image files
    /// </summary>
    public class ImageFileValidator : BaseFileValidator
    {
        private readonly ILogger<ImageFileValidator> _logger;
        private readonly List<string> _allowedMimeTypes;
        private readonly List<string> _allowedExtensions;

        /// <summary>
        /// Initializes a new instance of the ImageFileValidator class
        /// </summary>
        /// <param name="options">File upload configuration options</param>
        /// <param name="logger">Logger instance</param>
        public ImageFileValidator(
            IOptions<FileUploadOptions> options, 
            ILogger<ImageFileValidator> logger,
            IFileSystem fileSystem,
            ImageContentInspector contentInspector)
            : base(options, logger, fileSystem, contentInspector)
        {
            _logger = logger;
            _allowedMimeTypes = options.Value.ImageOptions.AllowedMimeTypes;
            _allowedExtensions = options.Value.ImageOptions.AllowedExtensions;
        }

        /// <summary>
        /// Checks if this validator can handle the given file type
        /// </summary>
        /// <param name="contentType">Content type to check</param>
        /// <returns>True if this validator can handle the content type, false otherwise</returns>
        public override bool CanHandleFileType(string contentType)
            => _contentInspector.CanHandleContentType(contentType);

        /// <summary>
        /// Performs image-specific validation
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the validation operation</returns>
        protected override async Task PerformFileTypeValidationAsync(IFormFileCollection files, CancellationToken cancellationToken)
        {
            // Check if all files are images
            foreach (IFormFile file in files)
            {
                if (!CanHandleFileType(file.ContentType))
                {
                    string errorMessage 
                        = $"File '{file.FileName}' is not an image. All files must be images in a single upload.";
                    _logger
                        .LogWarning(errorMessage);
                    throw new FileContentTypeException(errorMessage);
                }

                // Check if the image type is allowed
                if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                {
                    string errorMessage 
                        = $"Image type '{file.ContentType}' is not allowed. Allowed types are: {string.Join(", ", _allowedMimeTypes)}";
                    _logger
                        .LogWarning(errorMessage);
                    throw new FileContentTypeException(errorMessage);
                }

                // Check file extension
                string extension 
                    = _fileSystem.GetExtension(file.FileName).ToLowerInvariant();

                if (!_allowedExtensions.Contains(extension))
                {
                    string errorMessage 
                        = $"File extension '{extension}' is not allowed for images. Allowed extensions are: {string.Join(", ", _allowedExtensions)}";
                    _logger
                        .LogWarning(errorMessage);
                    throw new FileContentTypeException(FileConstants.ErrorMessages.InvalidFileType);
                }

                // Verify actual file content matches claimed type
                await VerifyImageContentAsync(file, cancellationToken);
            }
        }

        /// <summary>
        /// Verifies that the file content matches the claimed image type
        /// </summary>
        /// <param name="file">File to verify</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the verification operation</returns>
        private async Task VerifyImageContentAsync(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                bool isValid 
                    = await _contentInspector
                    .IsValidContentAsync(file, cancellationToken);
                
                if (!isValid)
                {
                    string contentType = file.ContentType.ToLowerInvariant();
                    string errorMessage 
                        = $"File '{file.FileName}' content does not match its claimed type '{contentType}'.";
                    _logger
                        .LogWarning(errorMessage);
                    throw new FileContentTypeException(FileConstants.ErrorMessages.ContentTypeMismatch);
                }
            }
            catch (FileContentTypeException)
            {
                throw; // Re-throw the specific exception
            }
            catch (Exception ex)
            {
                string errorMessage 
                    = $"Failed to verify content of file '{file.FileName}'.";
                _logger
                    .LogError(ex, errorMessage);
                throw new FileValidationException(errorMessage, ex);
            }
        }
    }
}
