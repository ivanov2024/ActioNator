using System.Text;
using ActioNator.Services.Exceptions;
using ActioNator.Services.Interfaces;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.ContentInspectors;
using ActioNator.GCommon;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Sharing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ActioNator.Services.Implementations.Cloud
{
    /// <summary>
    /// Dropbox-backed implementation for uploading and deleting user profile pictures.
    /// Credentials are supplied dynamically via method parameters to support runtime configuration.
    /// </summary>
    public class DropboxPictureService : IDropboxPictureService
    {
        private readonly ILogger<DropboxPictureService> _logger;
        private readonly ImageContentInspector _imageInspector;

        private static readonly IReadOnlyDictionary<string, string> ExtToMime
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".bmp"] = "image/bmp",
            [".tiff"] = "image/tiff",
        };

        public DropboxPictureService(ILogger<DropboxPictureService> logger, ImageContentInspector imageInspector)
        {
            _logger = logger;
            _imageInspector = imageInspector ?? throw new ArgumentNullException(nameof(imageInspector));
        }

        public async Task<string> UploadUserProfilePictureAsync(
            IFormFile file,
            string userId,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new FileValidationException("No file provided or file is empty.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Dropbox access token is required.", nameof(accessToken));

            // 1) Reject PDFs explicitly (by content-type or extension)
            var contentType = file.ContentType?.Trim().ToLowerInvariant();
            var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant() ?? string.Empty;

            if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, FileConstants.FileExtensions.Pdf, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidImageFormatException("PDF files are not allowed.");
            }

            // 2) Ensure MIME type is supported and is an image/*
            if (string.IsNullOrWhiteSpace(contentType) ||
                !FileConstants.ContentTypes.IsSupported(contentType) ||
                !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidImageFormatException(FileConstants.ErrorMessages.InvalidFileType);
            }

            // 3) Ensure extension is a supported image type
            if (string.IsNullOrWhiteSpace(extension) || !FileConstants.FileExtensions.IsSupportedImage(extension))
            {
                throw new InvalidImageFormatException(FileConstants.ErrorMessages.InvalidFileType);
            }

            // 4) Ensure MIME and extension consistency
            if (!ExtToMime.TryGetValue(extension, out var expectedMime) || !string.Equals(expectedMime, contentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidImageFormatException(FileConstants.ErrorMessages.ContentTypeMismatch);
            }

            // 5) Validate actual file bytes via content inspector (reject corrupted/suspicious files)
            if (!await _imageInspector.IsValidContentAsync(file, cancellationToken))
            {
                throw new InvalidImageFormatException(FileConstants.ErrorMessages.ContentTypeMismatch);
            }

            var safeFileName = GetSafeFileName(file.FileName);
            var dropboxPath = $"/users/{userId}/profile-picture/{safeFileName}"; // leading slash required

            try
            {
                using var dbx = new DropboxClient(accessToken);
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                // Upload (overwrite if exists)
                _logger
                .LogInformation("Uploading profile picture to Dropbox at path {Path}", dropboxPath);

                await dbx.Files.UploadAsync(
                    path: dropboxPath,
                    mode: WriteMode.Overwrite.Instance,
                    body: ms);

                // Ensure a shared link exists, then convert to direct URL suitable for <img>
                var sharedUrl = await GetOrCreateSharedLinkAsync(dbx, dropboxPath, cancellationToken);
                var directUrl = ToDirectContentUrl(sharedUrl);

                return directUrl;
            }
            catch (DropboxException ex)
            {
                _logger.LogError(ex, "Dropbox API error while uploading profile picture for user {UserId}", userId);
                throw new FileStorageException("Failed to upload the file to Dropbox.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading profile picture for user {UserId}", userId);
                throw new FileStorageException("An unexpected error occurred while uploading the file.", ex);
            }
        }

        public async Task<bool> DeleteUserProfilePictureAsync(
            string dropboxPath,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dropboxPath))
                throw new ArgumentException("Dropbox path is required.", nameof(dropboxPath));

            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("Dropbox access token is required.", nameof(accessToken));

            try
            {
                using var dbx = new DropboxClient(accessToken);
                var result = await dbx.Files.DeleteV2Async(path: dropboxPath);
                return result != null;
            }
            catch (DropboxException ex)
            {
                _logger.LogError(ex, "Dropbox API error while deleting file {Path}", dropboxPath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting file {Path}", dropboxPath);
                return false;
            }
        }

        private static bool IsImageContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return false;
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSafeFileName(string? fileName)
        {
            var name = string.IsNullOrWhiteSpace(fileName) ? "image" : Path.GetFileName(fileName);
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_' )
                    sb.Append(ch);
                else
                    sb.Append('_');
            }

            // Ensure extension exists
            var result = sb.ToString();
            if (!Path.HasExtension(result))
            {
                result += ".jpg"; // default extension when missing; actual content type decides rendering
            }
            return result;
        }

        private static async Task<string> GetOrCreateSharedLinkAsync(DropboxClient dbx, string dropboxPath, CancellationToken ct)
        {
            // Try to find existing shared link
            var list = await dbx.Sharing.ListSharedLinksAsync(path: dropboxPath, directOnly: true);
            var url = list?.Links?.FirstOrDefault()?.Url;
            if (!string.IsNullOrEmpty(url)) return url!;

            // Create new shared link
            var created = await dbx.Sharing.CreateSharedLinkWithSettingsAsync(path: dropboxPath);
            return created.Url;
        }

        private static string ToDirectContentUrl(string sharedUrl)
        {
            // Convert Dropbox shared link to direct content link
            // Example: https://www.dropbox.com/scl/fi/...?.dl=0 -> https://dl.dropboxusercontent.com/scl/fi/...?.raw=1
            if (string.IsNullOrEmpty(sharedUrl)) return sharedUrl;
            var uri = new Uri(sharedUrl);
            var host = uri.Host.Replace("www.dropbox.com", "dl.dropboxusercontent.com", StringComparison.OrdinalIgnoreCase);
            var baseWithoutQuery = $"{uri.Scheme}://{host}{uri.AbsolutePath}";

            var parsed = QueryHelpers.ParseQuery(uri.Query ?? string.Empty);
            var dict = new Dictionary<string, string?>();
            foreach (var kvp in parsed)
            {
                if (string.Equals(kvp.Key, "dl", StringComparison.OrdinalIgnoreCase))
                    continue;
                // Take the first value for simplicity
                dict[kvp.Key] = kvp.Value.FirstOrDefault();
            }
            dict["raw"] = "1";

            var newUrl = QueryHelpers.AddQueryString(baseWithoutQuery, dict!);
            return newUrl;
        }
    }
}
