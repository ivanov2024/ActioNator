namespace ActioNator.Services.Configuration
{
    /// <summary>
    /// Configuration options for a specific file type
    /// </summary>
    public class FileTypeOptions
    {
        /// <summary>
        /// Allowed MIME types for this file type
        /// </summary>
        public List<string> AllowedMimeTypes { get; set; } 
            = new ();

        /// <summary>
        /// Allowed file extensions for this file type
        /// </summary>
        public List<string> AllowedExtensions { get; set; } 
            = new ();
    }
}
