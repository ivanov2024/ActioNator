using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ActioNator.Services.Models;
using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Implementations.VerifyCoach
{
    /// <summary>
    /// Service for handling coach verification document uploads with enterprise-grade security and validation
    /// </summary>
    public class CoachDocumentUploadService : ICoachDocumentUploadService
    {
        private readonly IFileValidationOrchestrator _validationOrchestrator;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<CoachDocumentUploadService> _logger;
        private readonly FileUploadOptions _options;

        /// <summary>
        /// Initializes a new instance of the CoachDocumentUploadService class
        /// </summary>
        /// <param name="imageValidator">Validator for image files</param>
        /// <param name="pdfValidator">Validator for PDF files</param>
        /// <param name="fileStorageService">Service for storing files</param>
        /// <param name="options">File upload configuration options</param>
        /// <param name="logger">Logger instance</param>
        public CoachDocumentUploadService(
            IFileValidationOrchestrator validationOrchestrator,
            IFileStorageService fileStorageService,
            IFileSystem fileSystem,
            IOptions<FileUploadOptions> options,
            ILogger<CoachDocumentUploadService> logger)
        {
            _validationOrchestrator = validationOrchestrator ?? throw new ArgumentNullException(nameof(validationOrchestrator));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes the uploaded coach verification documents with enhanced security and validation
        /// </summary>
        /// <param name="files">Collection of files to upload</param>
        /// <param name="userId">ID of the user uploading the documents</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the upload operation with detailed information</returns>
        public async Task<FileUploadResponseModel> ProcessUploadAsync(
            IFormFileCollection files, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing {Count} files for user {UserId}", files?.Count ?? 0, userId);
            
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User identification failed for file upload");
                    return FileUploadResponseModel.CreateFailure("User identification failed.");
                }

                // Check total file size
                long totalSize = files.Sum(f => f.Length);
                if (totalSize > _options.MaxFileSize)
                {
                    _logger.LogWarning("Total file size {Size} bytes exceeds the maximum limit of {MaxSize} bytes for user {UserId}", 
                        totalSize, _options.MaxFileSize, userId);
                    
                    return FileUploadResponseModel.CreateFailure(
                        $"Total file size exceeds the maximum limit of {_options.MaxFileSize / (1024 * 1024)}MB.",
                        new ErrorDetailsModel   
                        {
                            ErrorType = "FileSizeExceeded",
                            ErrorMessage = "The total size of all files exceeds the allowed limit",
                            AdditionalInfo = new Dictionary<string, object>
                            {
                                { "TotalSize", totalSize },
                                { "MaxSize", _options.MaxFileSize },
                                { "SizeInMB", Math.Round((double)totalSize / (1024 * 1024), 2) },
                                { "MaxSizeInMB", Math.Round((double)_options.MaxFileSize / (1024 * 1024), 2) }
                            }
                        });
                }

                // Validate files using the orchestrator
                try
                {
                    _logger.LogInformation("Validating {Count} files for user {UserId}", files.Count, userId);
                    
                    var validationResult = await _validationOrchestrator.ValidateFilesAsync(files, cancellationToken);
                    
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("File validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                        
                        return FileUploadResponseModel.CreateFailure(
                            $"File validation failed: {validationResult.ErrorMessage}",
                            new ErrorDetailsModel
                            {
                                ErrorType = "FileValidationFailed",
                                ErrorMessage = validationResult.ErrorMessage,
                                AdditionalInfo = new Dictionary<string, object>
                                {
                                    { "ValidationMetadata", validationResult.ValidationMetadata }
                                }
                            });
                    }
                    
                    _logger.LogInformation("All files validated successfully for user {UserId}", userId);
                }
                catch (FileValidationException ex)
                {
                    _logger.LogWarning(ex, "File validation error for user {UserId}: {Message}", userId, ex.Message);
                    return FileUploadResponseModel.CreateFailure(
                        $"File validation error: {ex.Message}",
                        ErrorDetailsModel.FromException(ex));
                }

                // Save files to configured base path (already includes 'App_Data/coach-verifications')
                string uploadDir = _fileSystem.CombinePaths(_options.BasePath);
                IEnumerable<string> savedFilePaths 
                    = await _fileStorageService.SaveFilesAsync(files, uploadDir, userId, cancellationToken);

                // Create response with file details
                List<UploadedFileInfoModel> uploadedFiles 
                    = new ();

                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    var path = i < savedFilePaths.Count() ? savedFilePaths.ElementAt(i) : "unknown";
                    
                    uploadedFiles.Add(new UploadedFileInfoModel
                    {
                        OriginalFileName = file.FileName,
                        StoredFileName = _fileSystem.GetFileName(path),
                        FilePath = path,
                        FileSize = file.Length,
                        ContentType = file.ContentType
                    });
                }
                
                _logger.LogInformation("Successfully processed {Count} files for user {UserId}", files.Count, userId);
                
                return FileUploadResponseModel.CreateSuccess(
                    $"Successfully uploaded {files.Count} files.", 
                    uploadedFiles);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("File upload operation was cancelled for user {UserId}", userId);
                return FileUploadResponseModel.CreateFailure(
                    "The operation was cancelled.",
                    new ErrorDetailsModel { ErrorType = "OperationCancelled", ErrorMessage = "The operation was cancelled." });
            }
            catch (FileValidationException ex)
            {
                _logger.LogWarning(ex, "File validation error for user {UserId}: {Message}", userId, ex.Message);
                return FileUploadResponseModel.CreateFailure(
                    $"File validation error: {ex.Message}",
                    ErrorDetailsModel.FromException(ex));
            }
            catch (FileStorageException ex)
            {
                _logger.LogError(ex, "File storage error for user {UserId}: {Message}", userId, ex.Message);
                return FileUploadResponseModel.CreateFailure(
                    $"File storage error: {ex.Message}",
                    ErrorDetailsModel.FromException(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing files for user {UserId}", userId);
                return FileUploadResponseModel.CreateFailure(
                    "An unexpected error occurred while processing your request.",
                    ErrorDetailsModel.FromException(ex));
            }
        }
    }
}
