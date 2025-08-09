using ActioNator.Services.Interfaces.FileServices;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

using static ActioNator.GCommon.FileConstants;

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
            _signatureMap = new(StringComparer.OrdinalIgnoreCase)
            {
                { "image/jpeg", new[] { FileSignatures.Jpeg } },
                { "image/png", new[] { FileSignatures.Png } },
                { "image/gif", new[] { FileSignatures.Gif87a, FileSignatures.Gif89a } },
                { "image/webp", new[] { FileSignatures.WebP } },
                { "image/bmp", new[] { FileSignatures.Bmp } },
                { "image/tiff", new[] { FileSignatures.TiffI, FileSignatures.TiffM } },
                { "application/pdf", new[] { FileSignatures.Pdf } }
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
                await 
                    stream
                    .ReadAsync(header.AsMemory
                        (0, maxSignatureLength), cancellationToken);
                
                // Check if the header matches any of the signatures
                bool isValidHeader 
                    = signatures
                    .Any(signature 
                        => ByteArrayStartsWith(header, signature));
                
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
