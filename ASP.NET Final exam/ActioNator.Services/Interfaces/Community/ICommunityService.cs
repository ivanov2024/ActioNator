using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Interfaces.Community
{
    /// <summary>
    /// Defines operations for managing community posts, comments, and related actions.
    /// </summary>
    public interface ICommunityService
    {
        #region Post Retrieval

        /// <summary>
        /// Gets all posts for the community feed.
        /// </summary>
        /// <param name="userId">Current user ID (used for personalization, e.g., like status).</param>
        /// <param name="status">Optional filter by post status (null for all posts).</param>
        /// <returns>Collection of post view models.</returns>
        Task<IEnumerable<PostCardViewModel>> GetAllPostsAsync(Guid? userId, string status = null);

        /// <summary>
        /// Gets a post by ID.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="userId">Current user ID (used for personalization, e.g., like status).</param>
        /// <returns>Post view model.</returns>
        Task<PostCardViewModel> GetPostByIdAsync(Guid postId, Guid? userId);

        #endregion

        #region Post Creation

        /// <summary>
        /// Creates a new post.
        /// </summary>
        /// <param name="content">Post content.</param>
        /// <param name="userId">Author ID.</param>
        /// <returns>Created post view model.</returns>
        Task<PostCardViewModel> CreatePostAsync(string content, Guid userId);

        /// <summary>
        /// Creates a new post with images.
        /// </summary>
        /// <param name="content">Post content.</param>
        /// <param name="userId">Author ID.</param>
        /// <param name="images">List of image files to upload.</param>
        /// <returns>Created post view model.</returns>
        Task<PostCardViewModel> CreatePostAsync(string content, Guid userId, List<IFormFile> images);

        #endregion

        #region Post Interaction

        /// <summary>
        /// Likes or unlikes a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="userId">User ID performing the action.</param>
        /// <returns>New likes count.</returns>
        Task<int> ToggleLikePostAsync(Guid postId, Guid userId);

        /// <summary>
        /// Reports a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="reason">Report reason.</param>
        /// <param name="userId">User ID submitting the report.</param>
        /// <returns>Success status.</returns>
        Task<bool> ReportPostAsync(Guid postId, string reason, Guid userId);

        #endregion

        #region Comment Interaction

        /// <summary>
        /// Likes or unlikes a comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">User ID performing the action.</param>
        /// <returns>New likes count.</returns>
        Task<int> ToggleLikeCommentAsync(Guid commentId, Guid userId);

        /// <summary>
        /// Reports a comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="reason">Report reason.</param>
        /// <param name="userId">User ID submitting the report.</param>
        /// <returns>Success status.</returns>
        Task<bool> ReportCommentAsync(Guid commentId, string reason, Guid userId);

        #endregion

        #region Comment Management

        /// <summary>
        /// Adds a comment to a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="content">Comment content.</param>
        /// <param name="userId">Author ID.</param>
        /// <returns>Created comment view model.</returns>
        Task<PostCommentViewModel> AddCommentAsync(Guid postId, string content, Guid userId);

        /// <summary>
        /// Gets a comment by ID.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">Current user ID (for personalization).</param>
        /// <returns>Comment view model.</returns>
        Task<PostCommentViewModel> GetCommentByIdAsync(Guid commentId, Guid userId);

        /// <summary>
        /// Restores a deleted comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">User ID (for authorization).</param>
        /// <returns>Restored comment view model.</returns>
        Task<PostCommentViewModel> RestoreCommentAsync(Guid commentId, Guid userId);

        #endregion

        #region Moderation

        /// <summary>
        /// Deletes a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="userId">User ID (for authorization).</param>
        /// <returns>Success status.</returns>
        Task<bool> DeletePostAsync(Guid postId, Guid userId);

        /// <summary>
        /// Deletes a comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">User ID (for authorization).</param>
        /// <returns>Success status.</returns>
        Task<bool> DeleteCommentAsync(Guid commentId, Guid userId);

        #endregion
    }
}