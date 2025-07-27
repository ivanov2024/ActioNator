namespace ActioNator.GCommon
{
    /// <summary>
    /// Static class containing file-related constants used throughout the application
    /// </summary>
    public static class FileConstants
    {
        /// <summary>
        /// MIME content types for various file formats
        /// </summary>
        public static class ContentTypes
        {
            public const string Pdf = "application/pdf";
            public const string Jpeg = "image/jpeg";
            public const string Png = "image/png";
            public const string Gif = "image/gif";
            public const string Webp = "image/webp";
            public const string Bmp = "image/bmp";
            public const string Tiff = "image/tiff";
        }
        
        /// <summary>
        /// File extensions for various file formats
        /// </summary>
        public static class FileExtensions
        {
            public const string Pdf = ".pdf";
            public const string Jpeg = ".jpg";
            public const string JpegAlt = ".jpeg";
            public const string Png = ".png";
            public const string Gif = ".gif";
            public const string Webp = ".webp";
            public const string Bmp = ".bmp";
            public const string Tiff = ".tiff";
        }
        
        /// <summary>
        /// Common error messages for file operations
        /// </summary>
        public static class ErrorMessages
        {
            public const string NoFilesUploaded = "No files were uploaded.";
            public const string UserIdMissing = "User identification failed.";
            public const string FileSizeExceeded = "File size exceeds the maximum allowed limit.";
            public const string TotalSizeExceeded = "Total file size exceeds the maximum allowed limit.";
            public const string InvalidFileType = "Invalid file type. Only allowed types are accepted.";
            public const string ContentTypeMismatch = "File content does not match the declared content type.";
            public const string InvalidFileName = "File name contains invalid characters or patterns.";
            public const string UnauthorizedAccess = "You are not authorized to access this file.";
            public const string FileNotFound = "The requested file was not found.";
            public const string StorageError = "An error occurred while storing the file.";
            public const string MixedFileTypes = "Mixed file types detected. All files must be of the same type.";
            public const string ValidationFailed = "File validation failed. Please check the file and try again.";
        }
        
        /// <summary>
        /// Magic numbers for file content type detection
        /// </summary>
        public static class FileSignatures
        {
            // PDF signature
            public static readonly byte[] Pdf = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // %PDF
            
            // JPEG signatures
            public static readonly byte[] Jpeg = new byte[] { 0xFF, 0xD8, 0xFF };
            
            // PNG signature
            public static readonly byte[] Png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            
            // GIF signatures
            public static readonly byte[] Gif87a = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }; // GIF87a
            public static readonly byte[] Gif89a = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }; // GIF89a
            
            // WebP signature
            public static readonly byte[] WebP = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // RIFF
            
            // BMP signature
            public static readonly byte[] Bmp = new byte[] { 0x42, 0x4D }; // BM
            
            // TIFF signatures
            public static readonly byte[] TiffI = new byte[] { 0x49, 0x49, 0x2A, 0x00 }; // II*\0 (little endian)
            public static readonly byte[] TiffM = new byte[] { 0x4D, 0x4D, 0x00, 0x2A }; // MM\0* (big endian)
        }
    }
}
