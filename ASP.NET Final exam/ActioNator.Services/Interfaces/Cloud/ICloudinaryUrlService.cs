namespace ActioNator.Services.Interfaces.Cloud
{
    /// <summary>
    /// Defines operations for extracting data from Cloudinary URLs.
    /// </summary>
    public interface ICloudinaryUrlService
    {
        /// <summary>
        /// Extracts the Cloudinary public ID from a given Cloudinary image URL.
        /// </summary>
        /// <param name="cloudinaryUrl">The full Cloudinary URL of the image.</param>
        /// <returns>
        /// The public ID string that uniquely identifies the image in Cloudinary.
        /// </returns>
        string GetPublicId(string cloudinaryUrl);
    }

}
