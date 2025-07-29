using ActioNator.GCommon;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActioNator.Services
{
    /// <summary>
    /// Orchestrates file validation across multiple validators
    /// </summary>
    public class FileValidationOrchestrator : IFileValidationOrchestrator
    {
        private readonly IFileValidatorFactory _validatorFactory;
        private readonly ILogger<FileValidationOrchestrator> _logger;
        private readonly FileUploadOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the FileValidationOrchestrator class
        /// </summary>
        /// <param name="validatorFactory">Factory for getting appropriate validators</param>
        /// <param name="options">File upload configuration options</param>
        /// <param name="logger">Logger instance</param>
        public FileValidationOrchestrator(
            IFileValidatorFactory validatorFactory,
            IOptions<FileUploadOptions> options,
            ILogger<FileValidationOrchestrator> logger)
        {
            _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Validates a collection of files
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        public async Task<FileValidationResult> ValidateFilesAsync(
            IFormFileCollection files, 
            CancellationToken cancellationToken = default)
        {
            // Check if files collection is valid
            if (IsEmptyFileCollection(files))
            {
                return CreateNoFilesResult();
            }
            
            try
            {
                // Check total file size
                long totalSize = files.Sum(f => f.Length);
                if (totalSize > _options.MaxTotalSize)
                {
                    return CreateTotalSizeExceededResult(totalSize);
                }
                
                // Check if all files are of the same type
                if (!AreAllFilesSameType(files))
                {
                    _logger
                        .LogWarning("Files of different types were uploaded");

                    return FileValidationResult
                        .Failure(FileConstants.ErrorMessages.MixedFileTypes);
                }
                
                // Get the appropriate validator for the first file's content type
                IFormFile firstFile = files.First();
                IFileValidator validator 
                    = _validatorFactory.GetValidatorForFile(firstFile);
                
                // Validate all files using the selected validator
                return await validator.ValidateAsync(files, cancellationToken);
            }
            catch (Exception ex)
            {
                return HandleValidationException(ex);
            }
        }
        
        /// <summary>
        /// Validates a single file
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        public async Task<FileValidationResult> ValidateFileAsync(
            IFormFile file, 
            CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                return CreateNoFilesResult();
            }
            
            try
            {
                // Check file size
                if (file.Length > _options.MaxFileSize)
                {
                    return CreateFileSizeExceededResult(file);
                }
                
                // Get the appropriate validator for the file's content type
                IFileValidator validator = _validatorFactory.GetValidatorForFile(file);
                
                // Validate the file using the selected validator
                return await validator.ValidateAsync(file, cancellationToken);
            }
            catch (Exception ex)
            {
                return HandleValidationException(ex);
            }
        }
        
        /// <summary>
        /// Checks if all files in the collection are of the same type
        /// </summary>
        /// <param name="files">Files to check</param>
        /// <returns>True if all files are of the same type, false otherwise</returns>
        public bool AreAllFilesSameType(IFormFileCollection files)
        {
            if (files == null || !files.Any())
                return true;
                
            // Group files by content type category (image or pdf)
            IEnumerable<IFormFile> imageFiles 
                = files
                .Where(f => f.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

            IEnumerable<IFormFile> pdfFiles 
                = files
                .Where(f => f.ContentType.Equals(FileConstants.ContentTypes.Pdf, StringComparison.OrdinalIgnoreCase));
            
            // Check if all files are either images or PDFs, but not mixed
            return imageFiles.Count() == files.Count || pdfFiles.Count() == files.Count;
        }

        #region Private Helper Methods
        
        /// <summary>
        /// Checks if the file collection is null or empty
        /// </summary>
        private bool IsEmptyFileCollection(IFormFileCollection files)
            => files == null || !files.Any();
        
        
        /// <summary>
        /// Creates a validation result for when no files are provided
        /// </summary>
        private FileValidationResult CreateNoFilesResult()
        {
            _logger
                .LogWarning("No files provided for validation");

            return FileValidationResult
                .Failure(FileConstants.ErrorMessages.NoFilesUploaded);
        }
        
        /// <summary>
        /// Creates a validation result for when total file size exceeds the limit
        /// </summary>
        private FileValidationResult CreateTotalSizeExceededResult(long totalSize)
        {
            _logger
                .LogWarning("Total file size {Size} bytes exceeds the maximum limit of {MaxSize} bytes", 
                totalSize, _options.MaxTotalSize);
            
            return FileValidationResult
                .Failure(
                FileConstants.ErrorMessages.TotalSizeExceeded,
                new Dictionary<string, object>
                {
                    { "TotalSize", totalSize },
                    { "MaxAllowedSize", _options.MaxTotalSize }
                });
        }
        
        /// <summary>
        /// Creates a validation result for when a single file size exceeds the limit
        /// </summary>
        private FileValidationResult CreateFileSizeExceededResult(IFormFile file)
        {
            _logger
                .LogWarning("File size {Size} bytes exceeds the maximum limit of {MaxSize} bytes", 
                file.Length, _options.MaxFileSize);
            
            return FileValidationResult
                .Failure(
                FileConstants.ErrorMessages.FileSizeExceeded,
                new Dictionary<string, object>
                {
                    { "FileSize", file.Length },
                    { "MaxAllowedSize", _options.MaxFileSize },
                    { "FileName", file.FileName }
                });
        }
        
        /// <summary>
        /// Handles exceptions during validation and returns appropriate result
        /// </summary>
        private FileValidationResult HandleValidationException(Exception ex)
        {
            if (ex is FileValidationException fileValidationEx)
            {
                _logger
                    .LogWarning(fileValidationEx, "File validation failed: {Message}", fileValidationEx.Message);

                return FileValidationResult
                    .Failure(fileValidationEx.Message);
            }
            else
            {
                _logger
                    .LogError(ex, "Unexpected error during file validation");
                return FileValidationResult
                    .Failure($"Unexpected error during validation: {ex.Message}");
            }
        }
        
        #endregion
    }
}
