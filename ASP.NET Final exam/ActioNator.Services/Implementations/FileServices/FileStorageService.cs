using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using ActioNator.GCommon;

namespace ActioNator.Services.Implementations.FileServices
{
    /// <summary>
    /// Service for storing files
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FileStorageService> _logger;
        private readonly FileUploadOptions _options;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the FileStorageService class
        /// </summary>
        /// <param name="webHostEnvironment">Web host environment</param>
        /// <param name="options">File upload configuration options</param>
        /// <param name="logger">Logger instance</param>
        public FileStorageService(
            IWebHostEnvironment webHostEnvironment,
            IOptions<FileUploadOptions> options,
            ILogger<FileStorageService> logger,
            IFileSystem fileSystem)
        {
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        /// <summary>
        /// Saves files to the specified directory
        /// </summary>
        /// <param name="files">Files to save</param>
        /// <param name="relativePath">Relative path where files should be saved</param>
        /// <param name="userId">ID of the user who owns the files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of saved file paths</returns>
        public async Task<IEnumerable<string>> SaveFilesAsync(
            IFormFileCollection files, 
            string relativePath, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            if (files == null || !files.Any())
            {
                _logger.LogWarning("No files provided for saving");
                throw new FileStorageException("No files provided for saving");
            }

            if (string.IsNullOrEmpty(relativePath))
            {
                _logger.LogWarning("Path cannot be null or empty");
                throw new FileStorageException("Path cannot be null or empty");
            }

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID cannot be null or empty");
                throw new FileStorageException("User ID cannot be null or empty");
            }

            try
            {
                // Create user-specific directory if it doesn't exist
                string userBasedPath 
                    = _fileSystem.CombinePaths(relativePath, userId);
                string uploadDir = _fileSystem.CombinePaths(_webHostEnvironment.ContentRootPath, userBasedPath);
                
                // Ensure the directory exists
                _fileSystem.CreateDirectory(uploadDir);
                
                _logger
                    .LogInformation("Saving {Count} files to {Path} for user {UserId}", 
                    files.Count, userBasedPath, userId);

                List<string> savedFilePaths = new();

                // Save files
                foreach (IFormFile file in files)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();
                    
                    // Generate safe filename with timestamp and random component to avoid collisions
                    string safeFileName 
                        = GetSafeFileName(file.FileName);
                    string filePath 
                        = _fileSystem.CombinePaths(uploadDir, safeFileName);
                    
                    // Save file
                    using (Stream stream = _fileSystem.Create(filePath))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    // Store the relative path for return
                    string relativeSavedPath 
                        = _fileSystem.CombinePaths(userBasedPath, safeFileName);
                    savedFilePaths.Add(relativeSavedPath);
                }

                _logger
                    .LogInformation("Successfully saved {Count} files for user {UserId}", 
                    savedFilePaths.Count, userId);
                    
                return savedFilePaths;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("File saving operation was cancelled");
                throw; // Rethrow cancellation exceptions as-is
            }
            catch (Exception ex) when (ex is not FileStorageException)
            {
                _logger
                    .LogError(ex, "Error saving files to {Path} for user {UserId}", relativePath, userId);
                throw new FileStorageException("Failed to save files", ex);
            }
        }

        /// <summary>
        /// Generates a safe file name
        /// </summary>
        /// <param name="originalFileName">Original file name</param>
        /// <returns>Safe file name with extension</returns>
        public string GetSafeFileName(string originalFileName)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            string extension = Path.GetExtension(originalFileName);
            
            // Sanitize the file name
            string safeFileName = SanitizeFileName(fileNameWithoutExtension);
            
            // Ensure the filename isn't too long
            if (safeFileName.Length > 50)
            {
                safeFileName = safeFileName.Substring(0, 50);
            }
            
            // Add timestamp and random component to prevent collisions
            string timestamp 
                = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string randomComponent 
                = Guid.NewGuid().ToString().Substring(0, 8);
            
            return $"{safeFileName}_{timestamp}_{randomComponent}{extension}";
        }
        
        /// <summary>
        /// Sanitizes a file name by removing invalid characters and patterns
        /// </summary>
        /// <param name="fileName">File name to sanitize</param>
        /// <returns>Sanitized file name</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file";
                
            // Replace invalid characters with underscores
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized 
                = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Remove potentially dangerous patterns (e.g., ../, .\, etc.)
            sanitized 
                = System.Text.RegularExpressions.Regex.Replace(sanitized, @"(\.\.[\\/])|(\.[\\/])", "_");
            
            // Limit length
            if (sanitized.Length > 50)
                sanitized = sanitized.Substring(0, 50);
                
            return sanitized;
        }
        
        /// <summary>
        /// Retrieves a file with authorization check
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="userId">ID of the user requesting the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File stream and content type</returns>
        public async Task<(Stream FileStream, string ContentType)> GetFileAsync(
            string filePath, 
            string userId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new FileStorageException("File path cannot be null or empty");
            }
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new FileStorageException("User ID cannot be null or empty");
            }
            
            // Check if user is authorized to access this file
            bool isAuthorized = await IsUserAuthorizedForFileAsync(filePath, userId);
            if (!isAuthorized)
            {
                _logger
                    .LogWarning("User {UserId} attempted unauthorized access to file {FilePath}", userId, filePath);
                throw new UnauthorizedAccessException("You are not authorized to access this file");
            }
            
            try
            {
                // Get the full path
                string fullPath = _fileSystem
                    .CombinePaths(_webHostEnvironment.ContentRootPath, filePath);
                
                // Check if file exists
                if (!_fileSystem.FileExists(fullPath))
                {
                    _logger
                        .LogWarning("File {FilePath} not found", fullPath);
                    throw new FileNotFoundException(FileConstants.ErrorMessages.FileNotFound, fullPath);
                }
                
                // Determine content type based on extension
                string extension = _fileSystem
                    .GetExtension(fullPath).ToLowerInvariant();

                string contentType = extension switch
                {
                    FileConstants.FileExtensions.Pdf => FileConstants.ContentTypes.Pdf,
                    FileConstants.FileExtensions.Jpeg or FileConstants.FileExtensions.JpegAlt => FileConstants.ContentTypes.Jpeg,
                    FileConstants.FileExtensions.Png => FileConstants.ContentTypes.Png,
                    FileConstants.FileExtensions.Gif => FileConstants.ContentTypes.Gif,
                    FileConstants.FileExtensions.Webp => FileConstants.ContentTypes.Webp,
                    _ => "application/octet-stream"
                };
                
                // Open file stream
                Stream fileStream = _fileSystem.OpenRead(fullPath);
                
                return (fileStream, contentType);
            }
            catch (Exception ex) when (ex is not UnauthorizedAccessException && ex is not FileNotFoundException)
            {
                _logger
                    .LogError(ex, "Error retrieving file {FilePath} for user {UserId}", filePath, userId);
                throw new FileStorageException("Failed to retrieve file", ex);
            }
        }
        
        /// <summary>
        /// Checks if a user is authorized to access a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="userId">ID of the user requesting the file</param>
        /// <returns>True if the user is authorized, false otherwise</returns>
        public Task<bool> IsUserAuthorizedForFileAsync(string filePath, string userId)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(false);
            }
            
            try
            {
                // Check if the file path contains the user's ID folder
                // This ensures users can only access their own files
                string userFolder 
                    = Path.DirectorySeparatorChar + userId + Path.DirectorySeparatorChar;

                bool isAuthorized 
                    = filePath.Contains(userFolder, StringComparison.OrdinalIgnoreCase);
                
                if (!isAuthorized)
                {
                    _logger
                        .LogWarning("User {UserId} is not authorized to access {FilePath}", userId, filePath);
                }
                
                return Task.FromResult(isAuthorized);
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error checking authorization for file {FilePath} and user {UserId}", filePath, userId);
                return Task.FromResult(false);
            }
        }
    }
}
