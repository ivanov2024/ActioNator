using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Interfaces.FileServices
{
    /// <summary>
    /// Interface for orchestrating file validation across multiple validators
    /// </summary>
    public interface IFileValidationOrchestrator
    {
        /// <summary>
        /// Validates a collection of files
        /// </summary>
        /// <param name="files">Files to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<FileValidationResult> ValidateFilesAsync(
            IFormFileCollection files, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Validates a single file
        /// </summary>
        /// <param name="file">File to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<FileValidationResult> ValidateFileAsync(
            IFormFile file, 
            CancellationToken cancellationToken = default);
            
        /// <summary>
        /// Checks if all files in the collection are of the same type
        /// </summary>
        /// <param name="files">Files to check</param>
        /// <returns>True if all files are of the same type, false otherwise</returns>
        bool AreAllFilesSameType(IFormFileCollection files);
    }
}
