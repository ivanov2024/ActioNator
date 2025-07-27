using ActioNator.GCommon;
using ActioNator.Services.Configuration;
using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Validators
{
    /// <summary>
    /// Base class for file validators
    /// </summary>
    public abstract class BaseFileValidator : IFileValidator
    {
        private readonly ILogger<BaseFileValidator> _logger;
        protected readonly FileUploadOptions _options;
        protected readonly IFileSystem _fileSystem;
        protected readonly IFileContentInspector _contentInspector;
        
        // Regex for detecting path traversal attempts
        private static readonly Regex PathTraversalRegex = new Regex(@"\.{2,}|[/\\]", RegexOptions.Compiled);
        
        // Regex for sanitizing filenames - more comprehensive than the previous version
        private static readonly Regex FilenameCleanupRegex = new Regex(@"[^\w\.-]|[\\/:*?""<>|]|\.\.", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the BaseFileValidator class
        /// </summary>
        /// <param name="options">File upload configuration options</param>
        /// <param name="logger">Logger instance</param>
        protected BaseFileValidator(
            IOptions<FileUploadOptions> options, 
            ILogger<BaseFileValidator> logger,
            IFileSystem fileSystem,
            IFileContentInspector contentInspector)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _contentInspector = contentInspector ?? throw new ArgumentNullException(nameof(contentInspector));
        }

        /// <summary>
        /// Validates a collection of files
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        public virtual async Task<FileValidationResult> ValidateAsync(IFormFileCollection files, CancellationToken cancellationToken = default)
        {
            if (files == null || !files.Any())
            {
                _logger.LogWarning("No files provided for validation");
                return FileValidationResult.Failure(FileConstants.ErrorMessages.NoFilesUploaded);
            }

            // Check total size
            long totalSize = files.Sum(f => f.Length);
            if (totalSize > _options.MaxTotalSize)
            {
                string errorMessage = $"Total upload size ({totalSize / 1024 / 1024}MB) exceeds the maximum allowed ({_options.MaxTotalSize / 1024 / 1024}MB).";
                _logger.LogWarning(errorMessage);
                throw new FileSizeExceededException(FileConstants.ErrorMessages.TotalSizeExceeded);
            }

            // Check individual file sizes
            foreach (var file in files)
            {
                if (file.Length > _options.MaxFileSize)
                {
                    string errorMessage = $"File '{file.FileName}' ({file.Length / 1024 / 1024}MB) exceeds the maximum allowed file size ({_options.MaxFileSize / 1024 / 1024}MB).";
                    _logger.LogWarning(errorMessage);
                    throw new FileSizeExceededException(FileConstants.ErrorMessages.FileSizeExceeded);
                }
            }

            // Validate file names for security
            foreach (var file in files)
            {
                ValidateFileName(file.FileName);
            }

            // Perform file type specific validation
            await PerformFileTypeValidationAsync(files, cancellationToken);

            return FileValidationResult.Success();
        }
        
        /// <summary>
        /// Validates a single file
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        public virtual async Task<FileValidationResult> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                _logger.LogWarning("Null file provided for validation");
                return FileValidationResult.Failure(FileConstants.ErrorMessages.NoFilesUploaded);
            }
            
            // Check file size
            if (file.Length > _options.MaxFileSize)
            {
                string errorMessage = $"File '{file.FileName}' ({file.Length / 1024 / 1024}MB) exceeds the maximum allowed file size ({_options.MaxFileSize / 1024 / 1024}MB).";
                _logger.LogWarning(errorMessage);
                throw new FileSizeExceededException(errorMessage);
            }
            
            // Validate file name for security
            ValidateFileName(file.FileName);
            
            // Create a temporary collection with just this file for the file type validation
            var formFileCollection = new FormFileCollection { file };
            
            // Perform file type specific validation
            await PerformFileTypeValidationAsync(formFileCollection, cancellationToken);
            
            return FileValidationResult.Success();
        }

        /// <summary>
        /// Validates if a file name is safe
        /// </summary>
        /// <param name="fileName">File name to validate</param>
        protected virtual void ValidateFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new FileNameValidationException(FileConstants.ErrorMessages.InvalidFileName);
            }

            // Check for path traversal attempts
            if (PathTraversalRegex.IsMatch(Path.GetFileNameWithoutExtension(fileName)))
            {
                string errorMessage = $"File name '{fileName}' contains invalid characters that could be used for path traversal.";
                _logger.LogWarning(errorMessage);
                throw new FileNameValidationException(FileConstants.ErrorMessages.InvalidFileName);
            }

            // Check for dangerous extensions
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (IsDangerousExtension(extension))
            {
                string errorMessage = $"File with extension '{extension}' is not allowed for security reasons.";
                _logger.LogWarning(errorMessage);
                throw new FileNameValidationException(FileConstants.ErrorMessages.InvalidFileType);
            }
        }

        /// <summary>
        /// Checks if a file extension is considered dangerous
        /// </summary>
        /// <param name="extension">File extension to check</param>
        /// <returns>True if the extension is dangerous, false otherwise</returns>
        protected virtual bool IsDangerousExtension(string extension)
        {
            // Common dangerous extensions
            string[] dangerousExtensions = {
                ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".js",
                ".vbs", ".ps1", ".psm1", ".psd1", ".ps1xml", ".scf", ".lnk", ".inf",
                ".reg", ".application", ".gadget", ".msc", ".hta", ".cpl", ".msp",
                ".scr", ".ins", ".isp", ".pif", ".application", ".appref-ms", ".vbe",
                ".wsf", ".wsc", ".wsh", ".ps2", ".ps2xml", ".psc1", ".psc2", ".msh",
                ".msh1", ".msh2", ".mshxml", ".msh1xml", ".msh2xml", ".scpt", ".url"
            };

            return dangerousExtensions.Contains(extension);
        }

        /// <summary>
        /// Sanitizes a file name to make it safe for storage
        /// </summary>
        /// <param name="fileName">Original file name</param>
        /// <returns>Sanitized file name</returns>
        protected virtual string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return Guid.NewGuid().ToString();
            }

            // Remove any potentially dangerous characters
            string safeName = FilenameCleanupRegex.Replace(_fileSystem.GetFileName(fileName).Replace(_fileSystem.GetExtension(fileName), ""), "_");
            string extension = _fileSystem.GetExtension(fileName);

            // Ensure the filename isn't too long
            if (safeName.Length > 50)
            {
                safeName = safeName.Substring(0, 50);
            }

            return safeName + extension;
        }

        /// <summary>
        /// Performs file type specific validation
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the validation operation</returns>
        protected abstract Task PerformFileTypeValidationAsync(IFormFileCollection files, CancellationToken cancellationToken);

        /// <summary>
        /// Checks if this validator can handle the given file type
        /// </summary>
        /// <param name="contentType">Content type to check</param>
        /// <returns>True if this validator can handle the content type, false otherwise</returns>
        public abstract bool CanHandleFileType(string contentType);
    }
}
