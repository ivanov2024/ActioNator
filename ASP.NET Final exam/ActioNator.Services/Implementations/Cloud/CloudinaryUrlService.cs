using ActioNator.Services.Interfaces.Cloud;
using System.Text.RegularExpressions;

namespace ActioNator.Services.Implementations.Cloud
{
    /// <summary>
    /// Provides functionality to extract the Cloudinary public ID from image URLs.
    /// </summary>
    public class CloudinaryUrlService : ICloudinaryUrlService
    {
        /// <summary>
        /// Extracts the public ID from a full Cloudinary image URL.
        /// </summary>
        /// <param name="cloudinaryUrl">The complete URL of the Cloudinary image.</param>
        /// <returns>
        /// The public ID string uniquely identifying the image in Cloudinary,
        /// or an empty string if the input URL is null, empty, or the extraction fails.
        /// </returns>
        public string GetPublicId(string cloudinaryUrl)
        {
            if (string.IsNullOrWhiteSpace(cloudinaryUrl))
            {
                return string.Empty;
            }

            // Regular expression to extract the public ID from the Cloudinary URL.
            // It captures the segment after the version number (e.g., /v1234567890/)
            // up to but excluding the file extension.
            Regex regex
                = new(@"/v\d+/(.+?)\.(?:jpg|jpeg|png|gif|webp|bmp|tiff)$", RegexOptions.IgnoreCase);

            Match match = regex.Match(cloudinaryUrl);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}
