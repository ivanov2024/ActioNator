using ActioNator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services
{
    /// <summary>
    /// Service for validating uploaded files
    /// </summary>
    public class FileValidator : IFileValidator
    {
        private readonly string[] _allowedImageTypes;
        private readonly string[] _allowedPdfTypes;
        private readonly string[] _dangerousExtensions;

        /// <summary>
        /// Initializes a new instance of the FileValidator class
        /// </summary>
        public FileValidator()
        {
            _allowedImageTypes = new[] { "image/png", "image/jpg", "image/jpeg", "image/gif", "image/webp" };
            _allowedPdfTypes = new[] { "application/pdf" };
            _dangerousExtensions = new[] { 
                ".exe", ".dll", ".bat", ".cmd", ".com", ".js", ".vbs", 
                ".ps1", ".sh", ".php", ".asp", ".aspx", ".html", ".htm" 
            };
        }

        /// <summary>
        /// Validates a collection of files asynchronously
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the validation</returns>
        public Task<FileValidationResult> ValidateAsync(IFormFileCollection files, CancellationToken cancellationToken = default)
        {
            // This method now implements the interface method
            return Task.FromResult(ValidateFiles(files));
        }
        
        /// <summary>
        /// Validates a single file asynchronously
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the validation</returns>
        public Task<FileValidationResult> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            // Create a temporary collection with just this file
            var formFileCollection = new FormFileCollection { file };
            return Task.FromResult(ValidateFiles(formFileCollection));
        }

        /// <summary>
        /// Determines if this validator can handle the specified file type
        /// </summary>
        /// <param name="contentType">The content type to check</param>
        /// <returns>True if this validator can handle the file type, false otherwise</returns>
        public bool CanHandleFileType(string contentType)
        {
            // This validator handles both image and PDF files
            return _allowedImageTypes.Contains(contentType) || _allowedPdfTypes.Contains(contentType);
        }

        /// <summary>
        /// Validates a collection of files
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <returns>Result of the validation</returns>
        public FileValidationResult ValidateFiles(IFormFileCollection files)
        {
            if (files == null || !files.Any())
            {
                return FileValidationResult.Failure("No files were uploaded.");
            }

            // Check file type consistency
            bool hasImages = files.Any(f => _allowedImageTypes.Contains(f.ContentType));
            bool hasPdfs = files.Any(f => _allowedPdfTypes.Contains(f.ContentType));

            if (hasImages && hasPdfs)
            {
                return FileValidationResult.Failure("You cannot mix file types. Please upload either images or PDFs only.");
            }

            // Validate all files have allowed MIME types
            foreach (IFormFile file in files)
            {
                if (!_allowedImageTypes.Contains(file.ContentType) && !_allowedPdfTypes.Contains(file.ContentType))
                {
                    return FileValidationResult.Failure($"File type '{file.ContentType}' is not allowed.");
                }

                // Sanitize and validate filename
                string fileName = Path.GetFileName(file.FileName);
                if (string.IsNullOrEmpty(fileName) || ContainsMaliciousPathTraversal(fileName) || HasDangerousExtension(fileName))
                {
                    return FileValidationResult.Failure($"Invalid or potentially dangerous filename: {fileName}");
                }
            }

            return FileValidationResult.Success();
        }

        /// <summary>
        /// Checks if a filename contains malicious path traversal attempts
        /// </summary>
        /// <param name="fileName">Filename to check</param>
        /// <returns>True if the filename contains malicious path traversal, false otherwise</returns>
        private bool ContainsMaliciousPathTraversal(string fileName)
        {
            return fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\");
        }

        /// <summary>
        /// Checks if a filename has a dangerous extension
        /// </summary>
        /// <param name="fileName">Filename to check</param>
        /// <returns>True if the filename has a dangerous extension, false otherwise</returns>
        private bool HasDangerousExtension(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return _dangerousExtensions.Contains(extension);
        }
    }
}
