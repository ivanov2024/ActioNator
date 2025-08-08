namespace ActioNator.GCommon
{
    /// <summary>
    /// Static container for file-related constants and predefined collections used across the application.
    /// </summary>
    public static class FileConstants
    {
        /// <summary>
        /// MIME content types for various file formats.
        /// </summary>
        public static class ContentTypes
        {
            /// <summary>
            /// Supported MIME content types.
            /// </summary>
            public static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp",
                "image/bmp",
                "image/tiff"
            };

            /// <summary>
            /// Checks whether the specified content type is supported.
            /// </summary>
            public static bool IsSupported(string contentType) =>
                !string.IsNullOrWhiteSpace(contentType) && Supported.Contains(contentType);
        }

        /// <summary>
        /// Common file extensions for various formats.
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

            /// <summary>
            /// Supported file extensions for image validation.
            /// </summary>
            public static readonly HashSet<string> SupportedImages = new(StringComparer.OrdinalIgnoreCase)
            {
                Jpeg, JpegAlt, Png, Gif, Webp, Bmp, Tiff
            };

            public static bool IsSupportedImage(string extension) =>
                !string.IsNullOrWhiteSpace(extension) && SupportedImages.Contains(extension);
        }

        /// <summary>
        /// Centralized error messages for file operations.
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
        /// Potentially dangerous file extensions that should be blocked.
        /// </summary>
        public static class DangerousExtensions
        {
            public const string Exe = ".exe";
            public const string Dll = ".dll";
            public const string Bat = ".bat";
            public const string Cmd = ".cmd";
            public const string Com = ".com";
            public const string Js = ".js";
            public const string Vbs = ".vbs";
            public const string Ps1 = ".ps1";
            public const string Sh = ".sh";
            public const string Php = ".php";
            public const string Asp = ".asp";
            public const string Aspx = ".aspx";
            public const string Html = ".html";
            public const string Htm = ".htm";

            /// <summary>
            /// Set of blocked file extensions for security purposes.
            /// </summary>
            public static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
            {
                Exe, Dll, Bat, Cmd, Com, Js, Vbs, Ps1, Sh, Php, Asp, Aspx, Html, Htm
            };

            public static bool IsDangerous(string extension) =>
                !string.IsNullOrWhiteSpace(extension) && Blocked.Contains(extension);
        }

        /// <summary>
        /// File signatures (magic numbers) for content type detection.
        /// </summary>
        public static class FileSignatures
        {
            public static readonly byte[] Pdf = { 0x25, 0x50, 0x44, 0x46 }; // %PDF
            public static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };
            public static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            public static readonly byte[] Gif87a = { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 };
            public static readonly byte[] Gif89a = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
            public static readonly byte[] WebP = { 0x52, 0x49, 0x46, 0x46 }; // RIFF
            public static readonly byte[] Bmp = { 0x42, 0x4D }; // BM
            public static readonly byte[] TiffI = { 0x49, 0x49, 0x2A, 0x00 }; // II*\0
            public static readonly byte[] TiffM = { 0x4D, 0x4D, 0x00, 0x2A }; // MM\0*
        }
    }
}
