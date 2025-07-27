using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActioNator.Services
{
    /// <summary>
    /// Factory that provides appropriate file validators based on content type
    /// </summary>
    public class FileValidatorFactory : IFileValidatorFactory
    {
        private readonly IEnumerable<IFileValidator> _validators;
        private readonly ILogger<FileValidatorFactory> _logger;
        
        /// <summary>
        /// Initializes a new instance of the FileValidatorFactory class
        /// </summary>
        /// <param name="validators">Collection of available file validators</param>
        /// <param name="logger">Logger instance</param>
        public FileValidatorFactory(
            IEnumerable<IFileValidator> validators,
            ILogger<FileValidatorFactory> logger)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Gets a validator for the specified content type
        /// </summary>
        /// <param name="contentType">Content type to get validator for</param>
        /// <returns>File validator that can handle the content type</returns>
        public IFileValidator GetValidatorForContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentException("Content type cannot be null or empty", nameof(contentType));
            }
            
            var validator = _validators.FirstOrDefault(v => v.CanHandleFileType(contentType));
            
            if (validator == null)
            {
                _logger.LogWarning("No validator found for content type: {ContentType}", contentType);
                throw new FileValidationException($"No validator found for content type: {contentType}");
            }
            
            return validator;
        }
        
        /// <summary>
        /// Gets a validator for the specified file
        /// </summary>
        /// <param name="file">File to get validator for</param>
        /// <returns>File validator that can handle the file</returns>
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
