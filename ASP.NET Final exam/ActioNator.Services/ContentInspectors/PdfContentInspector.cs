using ActioNator.GCommon;
using ActioNator.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public string ContentType => FileConstants.ContentTypes.Pdf;
        
        /// <summary>
        /// Initializes a new instance of the PdfContentInspector class
        /// </summary>
        /// <param name="logger">Logger instance</param>
        public PdfContentInspector(ILogger<PdfContentInspector> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
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
                _logger.LogWarning("Cannot inspect null file");
                return false;
            }
            
            try
            {
                using var stream = file.OpenReadStream();
                
                // Check PDF signature
                byte[] header = new byte[FileConstants.FileSignatures.Pdf.Length];
                await stream.ReadAsync(header, 0, header.Length, cancellationToken);
                
                bool isValidHeader = ByteArrayStartsWith(header, FileConstants.FileSignatures.Pdf);
                
                if (!isValidHeader)
                {
                    _logger.LogWarning("File {FileName} has invalid PDF header", file.FileName);
                    return false;
                }
                
                // Check for JavaScript content which could be malicious
                // This is a simplified check - a real implementation would use a PDF parsing library
                stream.Position = 0;
                byte[] content = new byte[Math.Min(4096, stream.Length)]; // Read first 4KB or less
                await stream.ReadAsync(content, 0, content.Length, cancellationToken);
                
                string contentString = Encoding.ASCII.GetString(content);
                
                // Simple checks for potentially malicious content
                if (contentString.Contains("/JS") || contentString.Contains("/JavaScript") || 
                    contentString.Contains("/Launch") || contentString.Contains("/RichMedia"))
                {
                    _logger.LogWarning("File {FileName} contains potentially malicious JavaScript", file.FileName);
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
