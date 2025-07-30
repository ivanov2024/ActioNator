using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ActioNator.Services.Interfaces.FileServices;

namespace ActioNator.Services.Validators
{
    /// <summary>
    /// Factory for creating file validators based on content type
    /// </summary>
    public class FileValidatorFactory : IFileValidatorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the FileValidatorFactory class
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving validators</param>
        public FileValidatorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Gets a validator for the specified content type
        /// </summary>
        /// <param name="contentType">Content type to get validator for</param>
        /// <returns>File validator that can handle the content type</returns>
        /// <exception cref="InvalidOperationException">Thrown when no validator is found for the content type</exception>
        public IFileValidator GetValidatorForContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            contentType = contentType.ToLowerInvariant();

            // Get all registered validators
            IEnumerable<IFileValidator> validators 
                = _serviceProvider.GetServices<IFileValidator>();

            // Find the first validator that can handle this content type
            IFileValidator? validator 
                = validators
                .FirstOrDefault(v => v.CanHandleFileType(contentType));

            return validator ?? 
                throw new InvalidOperationException($"No validator found for content type: {contentType}");
        }

        /// <summary>
        /// Gets a validator for the specified file
        /// </summary>
        /// <param name="file">File to get validator for</param>
        /// <returns>File validator that can handle the file</returns>
        /// <exception cref="InvalidOperationException">Thrown when no validator is found for the file</exception>
        public IFileValidator GetValidatorForFile(IFormFile file)
            => file == null 
                ? throw new ArgumentNullException(nameof(file)) 
                : GetValidatorForContentType(file.ContentType);        
    }
}
