using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces
{
    /// <summary>
    /// Interface for validating uploaded files
    /// </summary>
    public interface IFileValidator
    {
        /// <summary>
        /// Validates a collection of files
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the validation</returns>
        Task<FileValidationResult> ValidateAsync(IFormFileCollection files, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates a single file
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the validation</returns>
        Task<FileValidationResult> ValidateAsync(IFormFile file, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if this validator can handle the given file type
        /// </summary>
        /// <param name="contentType">Content type to check</param>
        /// <returns>True if this validator can handle the content type, false otherwise</returns>
        bool CanHandleFileType(string contentType);
    }

    /// <summary>
    /// Represents the result of a file validation operation
    /// </summary>
    public class FileValidationResult
    {
        /// <summary>
        /// Indicates whether the validation was successful
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; private set; }
        
        /// <summary>
        /// Additional validation details or metadata
        /// </summary>
        public object ValidationMetadata { get; private set; }
        
        /// <summary>
        /// Private constructor to enforce factory method usage
        /// </summary>
        private FileValidationResult() { }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        /// <param name="metadata">Optional metadata about the validation</param>
        /// <returns>FileValidationResult instance</returns>
        public static FileValidationResult Success(object? metadata = null)
        {
            return new FileValidationResult
            {
                IsValid = true,
                ValidationMetadata = metadata
            };
        }

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="metadata">Optional metadata about the validation failure</param>
        /// <returns>FileValidationResult instance</returns>
        public static FileValidationResult Failure(string errorMessage, object? metadata = null)
        {
            return new FileValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                ValidationMetadata = metadata
            };
        }
    }
}
