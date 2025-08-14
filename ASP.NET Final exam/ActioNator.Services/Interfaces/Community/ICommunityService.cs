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
        Task<IReadOnlyList<PostCardViewModel>> GetAllPostsAsync
        (
            Guid userId,
            string status = null,
            int pageNumber = 1,
            int pageSize = 20,
            bool isAdmin = false,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Gets posts authored by a specific user for use on profile pages.
        /// </summary>
        /// <param name="currentUserId">Current user ID (used for personalization, e.g., like status).</param>
        /// <param name="authorId">The author/user ID whose posts to fetch.</param>
        /// <param name="status">Optional filter by post status (null for active public posts only unless admin).</param>
        /// <param name="pageNumber">Page number (1-based).</param>
        /// <param name="pageSize">Page size (1-100).</param>
        /// <param name="isAdmin">Whether current user is admin (controls visibility of deleted posts when requested).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of post view models by the specified author.</returns>
        Task<IReadOnlyList<PostCardViewModel>> GetPostsByAuthorAsync
        (
            Guid currentUserId,
            Guid authorId,
            string status = null,
            int pageNumber = 1,
            int pageSize = 20,
            bool isAdmin = false,
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Gets a post by ID.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="userId">Current user ID (used for personalization, e.g., like status).</param>
        /// <returns>Post view model.</returns>
        Task<PostCardViewModel?> GetPostByIdAsync(Guid postId, Guid userId);

        #endregion

        #region Post Creation

        /// <summary>
        /// Creates a new post with images.
        /// </summary>
        /// <param name="content">Post content.</param>
        /// <param name="userId">Author ID.</param>
        /// <param name="images">List of image files to upload.</param>
        /// <returns>Created post view model.</returns>
        Task<PostCardViewModel> CreatePostAsync
        (
            string content,
            Guid userId,
            CancellationToken
            cancellationToken,
            List<IFormFile>? images = null
        );

        #endregion

        #region Post Interaction

        /// <summary>
        /// Likes or unlikes a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="userId">User ID performing the action.</param>
        /// <returns>New likes count.</returns>
        Task<int> ToggleLikePostAsync
        (
            Guid postId,
            Guid userId,
            CancellationToken
            cancellationToken
            = default
        );

        /// <summary>
        /// Reports a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="reason">Report reason.</param>
        /// <param name="userId">User ID submitting the report.</param>
        /// <returns>Success status.</returns>
        Task<bool> ReportPostAsync
         (
             Guid postId,
             string reason,
             Guid userId,
             CancellationToken
             cancellationToken
             = default,
             string details = ""
         );

        #endregion

        #region Comment Interaction

        /// <summary>
        /// Likes or unlikes a comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">User ID performing the action.</param>
        /// <returns>New likes count.</returns>
        Task<int> ToggleLikeCommentAsync
        (
            Guid commentId,
            Guid userId,
            CancellationToken
            cancellationToken
            = default
        );

        /// <summary>
        /// Reports a comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="reason">Report reason.</param>
        /// <param name="userId">User ID submitting the report.</param>
        /// <returns>Success status.</returns>
        Task<bool> ReportCommentAsync
        (
            Guid commentId,
            string reason,
            Guid userId,
            CancellationToken
            cancellationToken = default,
            string details = ""
        );

        /// <summary>
        /// Reports a user.
        /// </summary>
        /// <param name="reportedUserId">ID of the user being reported.</param>
        /// <param name="reason">Report reason.</param>
        /// <param name="userId">User ID submitting the report.</param>
        /// <returns>Success status.</returns>
        Task<bool> ReportUserAsync
        (
            Guid reportedUserId,
            string reason,
            Guid userId,
            CancellationToken
            cancellationToken = default,
            string details = ""
        );

        #endregion

        #region Comment Management

        /// <summary>
        /// Adds a comment to a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="content">Comment content.</param>
        /// <param name="userId">Author ID.</param>
        /// <returns>Created comment view model.</returns>
        Task<PostCommentViewModel> AddCommentAsync
        (Guid postId,
            string content,
            Guid userId,
            CancellationToken
            cancellationToken =
            default
        );

        /// <summary>
        /// Gets a comment by ID.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">Current user ID (for personalization).</param>
        /// <returns>Comment view model.</returns>
        Task<PostCommentViewModel> GetCommentByIdAsync(Guid commentId, Guid userId);

        #endregion

        #region Moderation

        /// <summary>
        /// Deletes a post.
        /// </summary>
        /// <param name="postId">Post ID.</param>
        /// <param name="userId">User ID (for authorization).</param>
        /// <returns>Success status.</returns>
        Task<bool> DeletePostAsync
        (
            Guid postId,
            Guid userId,
            CancellationToken
            cancellationToken =
            default
        );

        /// <summary>
        /// Deletes a comment.
        /// </summary>
        /// <param name="commentId">Comment ID.</param>
        /// <param name="userId">User ID (for authorization).</param>
        /// <returns>Success status.</returns>
        Task<bool> DeleteCommentAsync
        (
            Guid commentId,
            Guid userId,
            CancellationToken
            cancellationToken =
            default
        );

        #endregion
    }
}