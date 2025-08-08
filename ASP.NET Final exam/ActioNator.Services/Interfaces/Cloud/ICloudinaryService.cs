using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.Cloud
{
    /// <summary>
    /// Defines operations for uploading and deleting images in Cloudinary
    /// that are associated with user posts.
    /// </summary>
    public interface ICloudinaryService
    {
        /// <summary>
        /// Uploads a single image to Cloudinary and associates it with a specific post.
        /// </summary>
        /// <param name="image">The image file to upload.</param>
        /// <param name="postId">The unique identifier of the post to associate the image with.</param>
        /// <param name="folder">
        /// Optional Cloudinary folder name for organizing uploads.
        /// Defaults to "community".
        /// </param>
        /// <returns>
        /// The secure URL of the uploaded image.
        /// </returns>
        Task<string> UploadImageAsync
        (
            IFormFile file,
            Guid postId,
            string folder = "community",
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Uploads multiple images to Cloudinary and associates them with a specific post.
        /// </summary>
        /// <param name="images">The collection of image files to upload.</param>
        /// <param name="postId">The unique identifier of the post to associate the images with.</param>
        /// <param name="folder">
        /// Optional Cloudinary folder name for organizing uploads.
        /// Defaults to "community".
        /// </param>
        /// <returns>
        /// A collection of secure URLs of the uploaded images.
        /// </returns>
        Task<IEnumerable<string>> UploadImagesAsync(IEnumerable<IFormFile> files, Guid postId, string folder = "community", CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all images from Cloudinary that are associated with a specific post.
        /// </summary>
        /// <param name="postId">The unique identifier of the post whose images should be deleted.</param>
        /// <returns>
        /// True if all deletions were successful; otherwise, false.
        /// </returns>
        Task<bool> DeleteImagesByPublicIdsAsync(Guid postId, List<string> publicIds, CancellationToken cancellationToken);
    }
}
