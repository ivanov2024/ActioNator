using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Interfaces
{
    /// <summary>
    /// Dropbox-based, reusable picture service focused on user profile pictures.
    /// Credentials are passed dynamically via method parameters to avoid appsettings coupling.
    /// </summary>
    public interface IDropboxPictureService
    {
        /// <summary>
        /// Upload a user's profile picture to Dropbox and return a public, display-ready URL.
        /// </summary>
        /// <param name="file">The uploaded image file.</param>
        /// <param name="userId">The user's ID (used to structure Dropbox paths).</param>
        /// <param name="accessToken">Dropbox OAuth 2 access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Public URL to the uploaded image suitable for direct rendering.</returns>
        /// <remarks>
        /// Performs image validation prior to upload (MIME/extension consistency, allowed types only, no PDFs,
        /// and header signature inspection). If validation fails, an <c>InvalidImageFormatException</c> is thrown.
        /// </remarks>
        Task<string> UploadUserProfilePictureAsync(
            IFormFile file,
            string userId,
            string accessToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a previously uploaded user profile picture from Dropbox by its Dropbox path.
        /// </summary>
        /// <param name="dropboxPath">The file path in Dropbox (e.g. /users/{userId}/profile-picture/xyz.jpg).</param>
        /// <param name="accessToken">Dropbox OAuth 2 access token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if deletion succeeded; otherwise false.</returns>
        Task<bool> DeleteUserProfilePictureAsync(
            string dropboxPath,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}
