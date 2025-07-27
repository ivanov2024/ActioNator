using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Interfaces
{
    /// <summary>
    /// Interface for a factory that provides appropriate file validators based on content type
    /// </summary>
    public interface IFileValidatorFactory
    {
        /// <summary>
        /// Gets a validator for the specified content type
        /// </summary>
        /// <param name="contentType">Content type to get validator for</param>
        /// <returns>File validator that can handle the content type</returns>
        IFileValidator GetValidatorForContentType(string contentType);
        
        /// <summary>
        /// Gets a validator for the specified file
        /// </summary>
        /// <param name="file">File to get validator for</param>
        /// <returns>File validator that can handle the file</returns>
        IFileValidator GetValidatorForFile(IFormFile file);
    }
}
