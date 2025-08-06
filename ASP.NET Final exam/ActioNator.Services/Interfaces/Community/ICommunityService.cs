using ActioNator.ViewModels.Posts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.Community
{
    public interface ICommunityService
    {
        /// <summary>
        /// Gets all posts for the community feed
        /// </summary>
        /// <param name="userId">Current user ID</param>
        /// <returns>Collection of post view models</returns>
        Task<IEnumerable<PostCardViewModel>> GetAllPostsAsync(string userId);

        /// <summary>
        /// Gets a post by ID
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="userId">Current user ID</param>
        /// <returns>Post view model</returns>
        Task<PostCardViewModel> GetPostByIdAsync(string postId, string userId);

        /// <summary>
        /// Creates a new post
        /// </summary>
        /// <param name="content">Post content</param>
        /// <param name="userId">Author ID</param>
        /// <returns>Created post ID</returns>
        Task<string> CreatePostAsync(string content, string userId);

        /// <summary>
        /// Likes or unlikes a post
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="userId">User ID</param>
        /// <returns>New likes count</returns>
        Task<int> ToggleLikePostAsync(string postId, string userId);

        /// <summary>
        /// Adds a comment to a post
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="content">Comment content</param>
        /// <param name="userId">Author ID</param>
        /// <returns>Created comment view model</returns>
        Task<PostCommentsViewModel> AddCommentAsync(string postId, string content, string userId);

        /// <summary>
        /// Deletes a post
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="userId">User ID (for authorization)</param>
        /// <returns>Success status</returns>
        Task<bool> DeletePostAsync(string postId, string userId);

        /// <summary>
        /// Deletes a comment
        /// </summary>
        /// <param name="commentId">Comment ID</param>
        /// <param name="userId">User ID (for authorization)</param>
        /// <returns>Success status</returns>
        Task<bool> DeleteCommentAsync(string commentId, string userId);

        /// <summary>
        /// Reports a post
        /// </summary>
        /// <param name="postId">Post ID</param>
        /// <param name="reason">Report reason</param>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> ReportPostAsync(string postId, string reason, string userId);

        /// <summary>
        /// Reports a comment
        /// </summary>
        /// <param name="commentId">Comment ID</param>
        /// <param name="reason">Report reason</param>
        /// <param name="userId">User ID</param>
        /// <returns>Success status</returns>
        Task<bool> ReportCommentAsync(string commentId, string reason, string userId);

        // TODO: Add methods for image upload functionality
    }
}