namespace ActioNator.Services.Models
{
    /// <summary>
    /// Information about an uploaded file
    /// </summary>
    public class UploadedFileInfo
    {
        /// <summary>
        /// Original file name
        /// </summary>
        public required string OriginalFileName { get; set; }

        /// <summary>
        /// Stored file name
        /// </summary>
        public required string StoredFileName { get; set; }

        /// <summary>
        /// Relative path to the file
        /// </summary>
        public required string FilePath { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// File content type
        /// </summary>
        public required string ContentType { get; set; }
    }
}
