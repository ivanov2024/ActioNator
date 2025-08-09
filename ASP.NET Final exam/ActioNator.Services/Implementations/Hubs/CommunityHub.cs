using ActioNator.Services.Interfaces.Community;
using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.SignalR;

namespace ActioNator.Hubs
{
    /// <summary>
    /// SignalR hub for real-time Community features
    /// </summary>
    public class CommunityHub : Hub
    {
        private readonly ICommunityService _communityService;
        
        public CommunityHub(ICommunityService communityService)
        {
            _communityService = communityService;
        }
        /// <summary>
        /// Broadcasts a new post to all connected clients
        /// </summary>
        /// <param name="post">The post view model to broadcast</param>
        public async Task BroadcastNewPost(PostCardViewModel post)
        {
            await Clients.All.SendAsync("ReceiveNewPost", post);
        }

        /// <summary>
        /// Broadcasts a new comment to all connected clients
        /// </summary>
        /// <param name="comment">The comment view model to broadcast</param>
        public async Task BroadcastNewComment(PostCommentViewModel comment)
        {
            await Clients.All.SendAsync("ReceiveNewComment", comment);
        }

        /// <summary>
        /// Broadcasts a post update (like, edit) to all connected clients
        /// </summary>
        /// <param name="postId">The ID of the updated post</param>
        /// <param name="likesCount">The new likes count</param>
        public async Task BroadcastPostUpdate(Guid postId, int likesCount)
        {
            await Clients.All.SendAsync("ReceivePostUpdate", postId, likesCount);
        }

        /// <summary>
        /// Broadcasts a comment update (like, edit) to all connected clients
        /// </summary>
        /// <param name="commentId">The ID of the updated comment</param>
        /// <param name="likesCount">The new likes count</param>
        public async Task BroadcastCommentUpdate(Guid commentId, int likesCount)
        {
            await Clients.All.SendAsync("ReceiveCommentUpdate", commentId, likesCount);
        }

        /// <summary>
        /// Broadcasts a post deletion to all connected clients
        /// </summary>
        /// <param name="postId">The ID of the deleted post</param>
        public async Task BroadcastPostDeletion(Guid postId)
        {
            await Clients.All.SendAsync("ReceivePostDeletion", postId);
        }

        /// <summary>
        /// Broadcasts a comment deletion to all connected clients
        /// </summary>
        /// <param name="commentId">The ID of the deleted comment</param>
        /// <param name="postId">The ID of the post containing the comment</param>
        public async Task BroadcastCommentDeletion(Guid commentId, Guid postId)
        {
            await Clients.All.SendAsync("ReceiveCommentDeletion", commentId, postId);
        }
        
        /// <summary>
        /// Adds a new comment to a post
        /// </summary>
        /// <param name="postId">The ID of the post</param>
        /// <param name="content">The comment content</param>
        /// <param name="parentCommentId">Optional parent comment ID for replies</param>
        public async Task AddComment(Guid postId, string content, Guid? parentCommentId = null)
        {
            try
            {
                // Get the current user ID from the connection context
                var userId = Context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new HubException("User not authenticated");
                }
                
                // Add the comment using the community service
                var comment = await _communityService.AddCommentAsync(postId, content, Guid.Parse(userId));
                
                // Broadcast the new comment to all clients
                await BroadcastNewComment(comment);
            }
            catch (Exception ex)
            {
                throw new HubException($"Error adding comment: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Likes or unlikes a comment
        /// </summary>
        /// <param name="commentId">The ID of the comment to like/unlike</param>
        public async Task LikeComment(Guid commentId)
        {
            try
            {
                // Get the current user ID from the connection context
                var userId = Context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new HubException("User not authenticated");
                }
                
                // Toggle the like using the community service
                var result = await _communityService.ToggleLikeCommentAsync(commentId, Guid.Parse(userId));
                
                // Broadcast the updated like count to all clients
                await BroadcastCommentUpdate(commentId, result);
            }
            catch (Exception ex)
            {
                throw new HubException($"Error liking comment: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Deletes a comment
        /// </summary>
        /// <param name="commentId">The ID of the comment to delete</param>
        public async Task DeleteComment(Guid commentId)
        {
            try
            {
                // Get the current user ID from the connection context
                var userId = Context.GetHttpContext().User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new HubException("User not authenticated");
                }
                
                // Get the post ID for the comment (needed for the broadcast)
                var comment = await _communityService.GetCommentByIdAsync(commentId, Guid.Parse(userId));
                if (comment == null)
                {
                    throw new HubException("Comment not found");
                }
                
                // Delete the comment using the community service
                var success = await _communityService.DeleteCommentAsync(commentId, Guid.Parse(userId));
                if (!success)
                {
                    throw new HubException("Failed to delete comment");
                }
                
                // Broadcast the comment deletion to all clients
                await BroadcastCommentDeletion(commentId, comment.PostId);
            }
            catch (Exception ex)
            {
                throw new HubException($"Error deleting comment: {ex.Message}");
            }
        }
    }
}
