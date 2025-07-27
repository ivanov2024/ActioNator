using System.Collections.Generic;

namespace ActioNator.Services.Configuration
{
    /// <summary>
    /// Configuration options for file upload functionality
    /// </summary>
    public class FileUploadOptions
    {
        /// <summary>
        /// The maximum allowed size for a single file in bytes
        /// </summary>
        public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB default

        /// <summary>
        /// The maximum allowed total size for all files in a single upload in bytes
        /// </summary>
        public long MaxTotalSize { get; set; } = 100 * 1024 * 1024; // 100MB default

        /// <summary>
        /// Base path for storing uploaded files relative to content root
        /// </summary>
        public string BasePath { get; set; } = "App_Data/coach-verifications";

        /// <summary>
        /// Configuration for image file uploads
        /// </summary>
        public FileTypeOptions ImageOptions { get; set; } = new FileTypeOptions();

        /// <summary>
        /// Configuration for PDF file uploads
        /// </summary>
        public FileTypeOptions PdfOptions { get; set; } = new FileTypeOptions();
    }

    /// <summary>
    /// Configuration options for a specific file type
    /// </summary>
    public class FileTypeOptions
    {
        /// <summary>
        /// Allowed MIME types for this file type
        /// </summary>
        public List<string> AllowedMimeTypes { get; set; } = new List<string>();

        /// <summary>
        /// Allowed file extensions for this file type
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new List<string>();
    }
}
