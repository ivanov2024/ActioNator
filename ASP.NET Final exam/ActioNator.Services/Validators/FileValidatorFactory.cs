using ActioNator.GCommon;
using ActioNator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

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
            var validators = _serviceProvider.GetServices<IFileValidator>();

            // Find the first validator that can handle this content type
            var validator = validators.FirstOrDefault(v => v.CanHandleFileType(contentType));

            if (validator == null)
            {
                throw new InvalidOperationException($"No validator found for content type: {contentType}");
            }

            return validator;
        }

        /// <summary>
        /// Gets a validator for the specified file
        /// </summary>
        /// <param name="file">File to get validator for</param>
        /// <returns>File validator that can handle the file</returns>
        /// <exception cref="InvalidOperationException">Thrown when no validator is found for the file</exception>
        public IFileValidator GetValidatorForFile(IFormFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return GetValidatorForContentType(file.ContentType);
        }
    }
}
