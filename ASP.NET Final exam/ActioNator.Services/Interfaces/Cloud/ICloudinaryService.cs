using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.Cloud
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Uploads an image to Cloudinary
        /// </summary>
        /// <param name="file">The image file to upload</param>
        /// <param name="folder">Optional folder name to organize uploads</param>
        /// <returns>The secure URL of the uploaded image</returns>
        Task<string> UploadImageAsync(IFormFile file, Guid postId, string folder = "community");
        
        /// <summary>
        /// Deletes an image from Cloudinary
        /// </summary>
        /// <param name="publicId">The public ID of the image to delete</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteImageAsync(string publicId);
        
        /// <summary>
        /// Extracts the public ID from a Cloudinary URL
        /// </summary>
        /// <param name="cloudinaryUrl">The Cloudinary URL</param>
        /// <returns>The public ID</returns>
        string GetPublicIdFromUrl(string cloudinaryUrl);
    }
}
