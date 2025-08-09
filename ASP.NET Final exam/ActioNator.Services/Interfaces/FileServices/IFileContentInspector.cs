using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Interfaces.FileServices
{
    /// <summary>
    /// Interface for file content inspection strategies
    /// </summary>
    public interface IFileContentInspector
    {
        /// <summary>
        /// Gets the content type this inspector handles
        /// </summary>
        string ContentType { get; }
        
        /// <summary>
        /// Checks if the file content is valid for the declared content type
        /// </summary>
        /// <param name="file">The file to inspect</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the content is valid, false otherwise</returns>
        Task<bool> IsValidContentAsync(IFormFile file, CancellationToken cancellationToken = default);
    }
}
