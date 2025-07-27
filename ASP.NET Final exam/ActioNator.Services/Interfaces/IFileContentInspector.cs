using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces
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
        
        /// <summary>
        /// Determines if this inspector can handle the given content type
        /// </summary>
        /// <param name="contentType">Content type to check</param>
        /// <returns>True if this inspector can handle the content type, false otherwise</returns>
        bool CanHandleContentType(string contentType);
    }
}
