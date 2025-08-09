using ActioNator.Services.Interfaces.FileServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

using static ActioNator.GCommon.FileConstants.ContentTypes;
using static ActioNator.GCommon.FileConstants.FileSignatures;

namespace ActioNator.Services.ContentInspectors
{
    /// <summary>
    /// Content inspector for PDF files
    /// </summary>
    public class PdfContentInspector : IFileContentInspector
    {
        private readonly ILogger<PdfContentInspector> _logger;
        
        /// <summary>
        /// Gets the content type this inspector handles
        /// </summary>
        public string ContentType => Supported
            .First(s => s == "application/pdf");
        
        /// <summary>
        /// Initializes a new instance of the PdfContentInspector class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public PdfContentInspector(ILogger<PdfContentInspector> logger)
            => _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        
        /// <summary>
        /// Checks if the file content is valid PDF content
        /// </summary>
        /// <param name="file">The file to inspect</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the content is valid PDF, false otherwise</returns>
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
                
                // Check PDF signature
                byte[] header = new byte[Pdf.Length];
                await stream
                    .ReadAsync(header, cancellationToken);
                
                bool isValidHeader 
                    = ByteArrayStartsWith(header, Pdf);
                
                if (!isValidHeader)
                {
                    _logger
                        .LogWarning("File {FileName} has invalid PDF header", file.FileName);
                    return false;
                }

                // Reset position
                stream.Position = 0;

                // Read first N bytes (more than 4KB if possible, but still efficient)
                byte[] buffer = new byte[Math.Min(16384, stream.Length)]; // 16 KB
                await stream.ReadAsync(buffer, cancellationToken);

                // Convert to lowercase to avoid missing case variants
                string contentString = Encoding.UTF8.GetString(buffer).ToLowerInvariant();

                // Suspicious patterns (lowercase)
                string[] dangerousKeywords =
                ["/js", "/javascript", "/launch", "/richmedia"];

                // Check if any dangerous keyword is present
                if (dangerousKeywords.Any(k => contentString.Contains(k)))
                {
                    _logger
                        .LogWarning("File {FileName} contains potentially malicious JavaScript", file.FileName);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inspecting PDF content for file {FileName}", file.FileName);
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
            return contentType?.Equals(ContentType, StringComparison.OrdinalIgnoreCase) == true;
        }
        
        /// <summary>
        /// Checks if a byte array starts with a specific signature
        /// </summary>
        /// <param name="array">The array to check</param>
        /// <param name="signature">The signature to look for</param>
        /// <returns>True if the array starts with the signature, false otherwise</returns>
        private bool ByteArrayStartsWith(byte[] array, byte[] signature)
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
    }
}
