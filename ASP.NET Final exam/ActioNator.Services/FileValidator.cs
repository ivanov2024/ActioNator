using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

using static ActioNator.GCommon.FileConstants.DangerousExtensions;
using static ActioNator.GCommon.FileConstants.FileExtensions;

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
            _allowedImageTypes = 
            [ 
               Jpeg, 
               Png,          
               Gif, 
               Webp,
               Bmp,
               Tiff,
            ];
            
            _allowedPdfTypes = [Pdf];
            
            _dangerousExtensions = [ 
                Exe,
                Dll,
                Bat,
                Cmd,
                Com,
                Js,
                Vbs,
                Ps1,
                Sh,
                Php,
                Asp,
                Aspx,
                Html,
                Htm
            ];
        }

        /// <summary>
        /// Validates a collection of files asynchronously
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the validation</returns>
        public Task<FileValidationResult> ValidateAsync(IFormFileCollection files, CancellationToken cancellationToken = default)
            => Task.FromResult(ValidateFiles(files));
 
        /// <summary>
        /// Validates a single file asynchronously
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the validation</returns>
        public Task<FileValidationResult> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            // Create a temporary collection with just this file
            FormFileCollection formFileCollection 
                = [file];

            return Task.FromResult(ValidateFiles(formFileCollection));
        }

        /// <summary>
        /// Determines if this validator can handle the specified file type
        /// </summary>
        /// <param name="contentType">The content type to check</param>
        /// <returns>True if this validator can handle the file type, false otherwise</returns>
        public bool CanHandleFileType(string contentType)
            => _allowedImageTypes
                .Contains(contentType) || 
                    _allowedPdfTypes.Contains(contentType);
        

        /// <summary>
        /// Validates a collection of files
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <returns>Result of the validation</returns>
        public FileValidationResult ValidateFiles(IFormFileCollection files)
        {
            if (files == null || !files.Any())
            {
                return FileValidationResult
                    .Failure("No files were uploaded.");
            }

            // Check file type consistency
            bool hasImages 
                = files
                .Any(f => _allowedImageTypes.Contains(f.ContentType));
            bool hasPdfs 
                = files
                .Any(f => _allowedPdfTypes.Contains(f.ContentType));

            if (hasImages && hasPdfs)
            {
                return FileValidationResult
                    .Failure("You cannot mix file types. Please upload either images or PDFs only.");
            }

            // Validate all files have allowed MIME types
            foreach (IFormFile file in files)
            {
                if (!_allowedImageTypes
                    .Contains(file.ContentType) 
                    && 
                    !_allowedPdfTypes
                    .Contains(file.ContentType))
                {
                    return FileValidationResult
                        .Failure($"File type '{file.ContentType}' is not allowed.");
                }

                // Sanitize and validate filename
                string fileName = Path.GetFileName(file.FileName);

                if (string.IsNullOrEmpty(fileName) || 
                    ContainsMaliciousPathTraversal(fileName) || 
                    HasDangerousExtension(fileName))
                {
                    return FileValidationResult
                        .Failure($"Invalid or potentially dangerous filename: {fileName}");
                }
            }

            return FileValidationResult.Success();
        }

        #region Private Helper Methods
        /// <summary>
        /// Checks if a filename contains malicious path traversal attempts
        /// </summary>
        /// <param name="fileName">Filename to check</param>
        /// <returns>True if the filename contains malicious path traversal, false otherwise</returns>
        private bool ContainsMaliciousPathTraversal(string fileName)
            => fileName.Contains("..") || 
            fileName.Contains("/") || 
            fileName.Contains("\\");
        

        /// <summary>
        /// Checks if a filename has a dangerous extension
        /// </summary>
        /// <param name="fileName">Filename to check</param>
        /// <returns>True if the filename has a dangerous extension, false otherwise</returns>
        private bool HasDangerousExtension(string fileName)
        {
            string extension 
                = Path.GetExtension(fileName).ToLowerInvariant();

            return _dangerousExtensions.Contains(extension);
        }

        #endregion
    }
}
