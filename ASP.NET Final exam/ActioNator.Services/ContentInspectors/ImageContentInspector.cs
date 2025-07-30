using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ActioNator.GCommon;
using ActioNator.Services.Interfaces.FileServices;

namespace ActioNator.Services.ContentInspectors
{
    /// <summary>
    /// Content inspector for image files
    /// </summary>
    public class ImageContentInspector : IFileContentInspector
    {
        private readonly ILogger<ImageContentInspector> _logger;
        private readonly Dictionary<string, byte[][]> _signatureMap;
        
        /// <summary>
        /// Gets the content type this inspector handles
        /// </summary>
        public string ContentType => "image/*";
        
        /// <summary>
        /// Initializes a new instance of the ImageContentInspector class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public ImageContentInspector(ILogger<ImageContentInspector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Initialize signature map for different image formats
            _signatureMap 
                = new (StringComparer.OrdinalIgnoreCase)
            {
                { FileConstants.ContentTypes.Jpeg, new[] { FileConstants.FileSignatures.Jpeg } },
                { FileConstants.ContentTypes.Png, new[] { FileConstants.FileSignatures.Png } },

                { FileConstants.ContentTypes.Gif, new[] 
                { FileConstants.FileSignatures.Gif87a, FileConstants.FileSignatures.Gif89a } },

                { FileConstants.ContentTypes.Webp, new[] { FileConstants.FileSignatures.WebP } },
                { FileConstants.ContentTypes.Bmp, new[] { FileConstants.FileSignatures.Bmp } },

                { FileConstants.ContentTypes.Tiff, new[] 
                { FileConstants.FileSignatures.TiffI, FileConstants.FileSignatures.TiffM } }
            };
        }
        
        /// <summary>
        /// Checks if the file content is valid image content
        /// </summary>
        /// <param name="file">The file to inspect</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the content is a valid image, false otherwise</returns>
        public async Task<bool> IsValidContentAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null)
            {
                _logger
                    .LogWarning("Cannot inspect null file");
                return false;
            }
            
            try
            {
                using Stream stream = file.OpenReadStream();
                
                // Get the declared content type
                string contentType 
                    = file
                    .ContentType
                    .ToLowerInvariant();
                
                // Get the appropriate signatures to check
                if (!_signatureMap
                    .TryGetValue(contentType, out byte[][]? signatures))
                {
                    _logger
                        .LogWarning("No signature defined for content type {ContentType}", contentType);
                    return false;
                }
                
                // Get the longest signature length to ensure we read enough bytes
                int maxSignatureLength = signatures.Max(s => s.Length);
                byte[] header = new byte[maxSignatureLength];
                
                // Read the header
                await stream
                    .ReadAsync(header, 0, maxSignatureLength, cancellationToken);
                
                // Check if the header matches any of the signatures
                bool isValidHeader 
                    = signatures
                    .Any(signature => ByteArrayStartsWith(header, signature));
                
                if (!isValidHeader)
                {
                    _logger
                        .LogWarning("File {FileName} has invalid image header for content type {ContentType}", 
                        file.FileName, contentType);
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error inspecting image content for file {FileName}", file.FileName);
                return false;
            }
        }
        
        /// <summary>
        /// Determines if this inspector can handle the given content type
        /// </summary>
        /// <param name="contentType">Content type to check</param>
        /// <returns>True if this inspector can handle the content type, false otherwise</returns>
        public bool CanHandleContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            contentType = contentType.ToLowerInvariant();
            
            // Check if it's a specific image type we support 
            // or if it's a generic image type
            return _signatureMap.ContainsKey(contentType) || 
            contentType.StartsWith("image/");
        }

        #region Private Helper Method
        /// <summary>
        /// Checks if a byte array starts with a specific signature
        /// </summary>
        /// <param name="array">The array to check</param>
        /// <param name="signature">The signature to look for</param>
        /// <returns>True if the array starts with the signature, false otherwise</returns>
        private static bool ByteArrayStartsWith(byte[] array, byte[] signature)
        {
            if (array.Length < signature.Length)
                return false;
                
            for (int i = 0; i < signature.Length; i++)
            {
                if (array[i] != signature[i])
                    return false;
            }
            
            return true;
        }

        #endregion
    }
}
