using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.SignalR;

namespace ActioNator.Hubs
{
    /// <summary>
    /// SignalR hub for real-time Community features
    /// </summary>
    public class CommunityHub : Hub
    {
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
    }
}
