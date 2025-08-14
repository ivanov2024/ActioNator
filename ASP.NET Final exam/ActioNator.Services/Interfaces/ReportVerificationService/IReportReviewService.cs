using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActioNator.ViewModels.Reports;

namespace ActioNator.Services.Interfaces.ReportVerificationService
{
    /// <summary>
    /// Service for reviewing and managing reported content
    /// </summary>
    public interface IReportReviewService
    {
        /// <summary>
        /// Gets all reported posts
        /// </summary>
        /// <returns>List of reported posts</returns>
        Task<List<ReportedPostViewModel>> GetReportedPostsAsync();
        
        /// <summary>
        /// Gets all reported comments
        /// </summary>
        /// <returns>List of reported comments</returns>
        Task<List<ReportedCommentViewModel>> GetReportedCommentsAsync();
        
        /// <summary>
        /// Gets all reported users
        /// </summary>
        /// <returns>List of reported users</returns>
        Task<List<ReportedUserViewModel>> GetReportedUsersAsync();
        
        /// <summary>
        /// Gets the number of distinct posts that currently have pending reports.
        /// Excludes deleted posts.
        /// </summary>
        /// <returns>Count of posts with pending reports</returns>
        Task<int> GetPendingPostReportsCountAsync();
        
        /// <summary>
        /// Gets the number of distinct comments that currently have pending reports.
        /// Excludes deleted comments.
        /// </summary>
        /// <returns>Count of comments with pending reports</returns>
        Task<int> GetPendingCommentReportsCountAsync();
        
        /// <summary>
        /// Deletes a post and its associated reports
        /// </summary>
        /// <param name="postId">ID of the post to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeletePostAsync(Guid postId);
        
        /// <summary>
        /// Deletes a comment and its associated reports
        /// </summary>
        /// <param name="commentId">ID of the comment to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteCommentAsync(Guid commentId);
        
        /// <summary>
        /// Soft-deletes a user (marks as deleted) and removes associated user reports
        /// </summary>
        /// <param name="userId">ID of the user to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteUserAsync(Guid userId);
        
        /// <summary>
        /// Dismisses reports for a post without deleting the post
        /// </summary>
        /// <param name="postId">ID of the post</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DismissPostReportAsync(Guid postId);
        
        /// <summary>
        /// Dismisses reports for a comment without deleting the comment
        /// </summary>
        /// <param name="commentId">ID of the comment</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DismissCommentReportAsync(Guid commentId);

        /// <summary>
        /// Dismisses reports for a user without deleting the user
        /// </summary>
        /// <param name="userId">ID of the user</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DismissUserReportAsync(Guid userId);
    }
}
