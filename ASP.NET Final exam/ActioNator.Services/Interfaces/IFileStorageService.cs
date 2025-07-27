using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces
{
    /// <summary>
    /// Interface for storing files
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves files to the specified directory
        /// </summary>
        /// <param name="files">Files to save</param>
        /// <param name="relativePath">Relative path where files should be saved</param>
        /// <param name="userId">ID of the user who owns the files</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of saved file paths</returns>
        Task<IEnumerable<string>> SaveFilesAsync(IFormFileCollection files, string relativePath, string userId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Generates a safe file name
        /// </summary>
        /// <param name="fileName">Original file name</param>
        /// <returns>Safe file name</returns>
        string GetSafeFileName(string fileName);
        
        /// <summary>
        /// Retrieves a file with authorization check
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="userId">ID of the user requesting the file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>File stream and content type</returns>
        Task<(Stream FileStream, string ContentType)> GetFileAsync(string filePath, string userId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Checks if a user is authorized to access a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="userId">ID of the user requesting the file</param>
        /// <returns>True if the user is authorized, false otherwise</returns>
        Task<bool> IsUserAuthorizedForFileAsync(string filePath, string userId);
    }
}
