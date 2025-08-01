using ActioNator.Services.Exceptions;
using Microsoft.Extensions.Logging;
using ActioNator.Services.Models;
using Microsoft.AspNetCore.Http;
using ActioNator.GCommon;
using ActioNator.Services.Interfaces.FileServices;

namespace ActioNator.Services.Validators
{
    /// <summary>
    /// Orchestrates file validation using the appropriate validator based on file type
    /// </summary>
    public class FileValidationOrchestrator : IFileValidationOrchestrator
    {
        private readonly IFileValidatorFactory _validatorFactory;
        private readonly ILogger<FileValidationOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of the FileValidationOrchestrator class
        /// </summary>
        /// <param name="validatorFactory">Factory for creating file validators</param>
        /// <param name="logger">Logger instance</param>
        public FileValidationOrchestrator(
            IFileValidatorFactory validatorFactory,
            ILogger<FileValidationOrchestrator> logger)
        {
            _validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates a collection of files, ensuring they are all of the same type
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        public async Task<FileValidationResult> ValidateFilesAsync(
            IFormFileCollection files,
            CancellationToken cancellationToken = default)
        {
            if (files == null || !files.Any())
            {
                _logger.LogWarning("No files were provided for validation");
                return FileValidationResult.Failure(FileConstants.ErrorMessages.NoFilesUploaded);
            }

            try
            {
                // Determine file type from the first file
                IFormFile? firstFile = files.First();
                string contentType = firstFile.ContentType.ToLowerInvariant();

                // Get the appropriate validator
                FileValidationResult? validatorResult = GetValidatorForContentType(contentType);
                if (!validatorResult.IsValid)
                {
                    return validatorResult;
                }
                
                IFileValidator validator = (IFileValidator)((Dictionary<string, object>)validatorResult.ValidationMetadata)["validator"];

                // Check if all files are of the same type
                foreach (IFormFile file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!validator.CanHandleFileType(file.ContentType))
                    {
                        _logger
                            .LogWarning("Mixed file types detected. Expected {ExpectedType} but found {ActualType}",
                            contentType, file.ContentType);

                        return FileValidationResult
                            .Failure(FileConstants.ErrorMessages.MixedFileTypes,
                            new Dictionary<string, object>
                            {
                                { "ExpectedType", contentType },
                                { "FoundType", file.ContentType }
                            });
                    }
                }

                // Validate each file
                foreach (IFormFile file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger
                        .LogInformation("Validating file {FileName} ({FileSize} bytes)",
                        file.FileName, file.Length);

                    var validationResult 
                        = await validator.ValidateAsync(file, cancellationToken);
                    if (!validationResult.IsValid)
                    {
                        _logger
                            .LogWarning("File validation failed for {FileName}: {ErrorMessage}",
                            file.FileName, validationResult.ErrorMessage);

                        // Create new validation result with file information
                        Dictionary<string, object> metadata 
                            = new()
                            { 
                                { "FileName", file.FileName },
                                { "FileSize", file.Length }
                            };
                        
                        // Return the failure result with additional metadata
                        return FileValidationResult
                            .Failure(validationResult.ErrorMessage, metadata);
                    }
                }

                _logger
                    .LogInformation("All {Count} files validated successfully", files.Count);
                return FileValidationResult.Success();
            }
            catch (OperationCanceledException)
            {
                _logger
                    .LogInformation("File validation operation was canceled");
                throw;
            }
            catch (FileValidationException ex)
            {
                _logger
                    .LogWarning(ex, "File validation exception: {Message}", ex.Message);
                return FileValidationResult
                    .Failure(ex.Message, ErrorDetailsModel.FromException(ex).AdditionalInfo);
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Unexpected error during file validation");
                throw new FileValidationException(FileConstants.ErrorMessages.ValidationFailed, ex);
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
                _logger
                    .LogWarning("No file was provided for validation");
                return FileValidationResult
                    .Failure(FileConstants.ErrorMessages.NoFilesUploaded);
            }

            try
            {
                string contentType = file.ContentType.ToLowerInvariant();

                // Get the appropriate validator
                FileValidationResult? validatorResult = GetValidatorForContentType(contentType);
                if (!validatorResult.IsValid)
                {
                    return validatorResult;
                }
                
                IFileValidator validator = (IFileValidator)((Dictionary<string, object>)validatorResult.ValidationMetadata)["validator"];

                _logger
                    .LogInformation("Validating file {FileName} ({FileSize} bytes)",
                    file.FileName, file.Length);

                FileValidationResult? validationResult 
                    = await validator.ValidateAsync(file, cancellationToken);
                if (!validationResult.IsValid)
                {
                    _logger
                        .LogWarning("File validation failed for {FileName}: {ErrorMessage}",
                        file.FileName, validationResult.ErrorMessage);

                    // Create new validation result with file information
                    Dictionary<string, object> metadata 
                        = new ()
                    {
                        { "FileName", file.FileName },
                        { "FileSize", file.Length }
                    };
                    
                    // Return the failure result with additional metadata
                    return FileValidationResult
                        .Failure(validationResult.ErrorMessage, metadata);
                }

                _logger
                    .LogInformation("File {FileName} validated successfully", file.FileName);
                return FileValidationResult.Success();
            }
            catch (OperationCanceledException)
            {
                _logger
                    .LogInformation("File validation operation was canceled");
                throw;
            }
            catch (FileValidationException ex)
            {
                _logger
                    .LogWarning(ex, "File validation exception: {Message}", ex.Message);
                return FileValidationResult
                    .Failure(ex.Message, ErrorDetailsModel.FromException(ex).AdditionalInfo);
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Unexpected error during file validation");
                throw new FileValidationException(FileConstants.ErrorMessages.ValidationFailed, ex);
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
            {
                return true; // Empty collection is considered to be of the same type
            }

            if (files.Count == 1)
            {
                return true; // Single file is always of the same type
            }

            IFormFile firstFile = files.First();
            string firstContentType = firstFile.ContentType.ToLowerInvariant();

            // Check if first file is an image or PDF
            bool isFirstImage = firstContentType.StartsWith("image/");
            bool isFirstPdf = firstContentType == FileConstants.ContentTypes.Pdf;

            // Check all other files
            foreach (IFormFile file in files.Skip(1))
            {
                string contentType = file.ContentType.ToLowerInvariant();
                bool isImage = contentType.StartsWith("image/");
                bool isPdf = contentType == FileConstants.ContentTypes.Pdf;

                // If file types don't match the first file's type, return false
                if ((isFirstImage && !isImage) || (isFirstPdf && !isPdf))
                {
                    return false;
                }
            }

            return true;
        }

        #region Private Helper Methods
        
        /// <summary>
        /// Gets the appropriate validator for the specified content type
        /// </summary>
        /// <param name="contentType">Content type to get validator for</param>
        /// <returns>Success result with validator or failure result with error details</returns>
        private FileValidationResult GetValidatorForContentType(string contentType)
        {
            try
            {
                IFileValidator validator = _validatorFactory
                    .GetValidatorForContentType(contentType);
                    
                return FileValidationResult.Success(new Dictionary<string, object>
                {
                    { "validator", validator }
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger
                    .LogWarning(ex, "Unsupported file type: {ContentType}", contentType);
                    
                return FileValidationResult
                    .Failure($"Unsupported file type: {contentType}. Only images and PDFs are allowed.",
                    new Dictionary<string, object>
                    {
                        { "ContentType", contentType },
                        { "AllowedTypes", "images, pdf" }
                    });
            }
        }
        
        #endregion
    }
}
