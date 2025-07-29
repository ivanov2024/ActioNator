using ActioNator.Services.ContentInspectors;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ActioNator.GCommon;

namespace ActioNator.Services.Validators
{
    /// <summary>
    /// Validator for PDF files
    /// </summary>
    public class PdfFileValidator : BaseFileValidator
    {
        private readonly ILogger<PdfFileValidator> _logger;

        /// <summary>
        /// Initializes a new instance of the PdfFileValidator class
        /// </summary>
        /// <param name="options">File upload configuration options</param>
        /// <param name="logger">Logger instance</param>
        public PdfFileValidator(
            IOptions<FileUploadOptions> options, 
            ILogger<PdfFileValidator> logger,
            IFileSystem fileSystem,
            PdfContentInspector contentInspector)
            : base(options, logger, fileSystem, contentInspector)
        {
            _logger = logger;
        }

        /// <summary>
        /// Checks if this validator can handle the given file type
        /// </summary>
        /// <param name="contentType">Content type to check</param>
        /// <returns>True if this validator can handle the content type, false otherwise</returns>
        public override bool CanHandleFileType(string contentType)
            => _contentInspector.CanHandleContentType(contentType);

        /// <summary>
        /// Performs PDF-specific validation
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the validation operation</returns>
        protected override async Task PerformFileTypeValidationAsync(IFormFileCollection files, CancellationToken cancellationToken)
        {
            // Check if all files are PDFs
            foreach (IFormFile file in files)
            {
                if (!CanHandleFileType(file.ContentType))
                {
                    string errorMessage 
                        = $"File '{file.FileName}' is not a PDF. All files must be PDFs in a single upload.";
                    _logger
                        .LogWarning(errorMessage);
                    throw new FileContentTypeException(FileConstants.ErrorMessages.InvalidFileType);
                }

                // Check file extension
                string extension 
                    = _fileSystem
                    .GetExtension(file.FileName)
                    .ToLowerInvariant();

                if (extension != FileConstants.FileExtensions.Pdf)
                {
                    string errorMessage 
                        = $"File extension '{extension}' is not allowed for PDFs. Only .pdf is allowed.";
                    _logger
                        .LogWarning(errorMessage);
                    throw new FileContentTypeException(FileConstants.ErrorMessages.InvalidFileType);
                }

                // Verify actual file content matches PDF format
                await VerifyPdfContentAsync(file, cancellationToken);
            }
        }

        #region Private Helper Method
        /// <summary>
        /// Verifies that the file content is a valid PDF
        /// </summary>
        /// <param name="file">File to verify</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the verification operation</returns>
        private async Task VerifyPdfContentAsync(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                bool isValid 
                    = await _contentInspector
                    .IsValidContentAsync(file, cancellationToken);
                
                if (!isValid)
                {
                    string errorMessage 
                        = $"File '{file.FileName}' is not a valid PDF document.";
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
                    = $"Failed to verify content of PDF file '{file.FileName}'.";
                _logger
                    .LogError(ex, errorMessage);
                throw new FileValidationException(errorMessage, ex);
            }
        }

        #endregion
    }
}
