using ActioNator.Data;
using ActioNator.Services.Interfaces.ReportVerificationService;
using ActioNator.ViewModels.Reports;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActioNator.Services.Implementations.ReportVerificationService
{
    public class ReportReviewService : IReportReviewService
    {
        private readonly ActioNatorDbContext _dbContext;

        public ReportReviewService(ActioNatorDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ReportedPostViewModel>> GetReportedPostsAsync()
        {
            // Query the database for reported posts
            var reportedPosts = await _dbContext.PostReports
                .GroupBy(r => r.PostId)
                .Select(g => new ReportedPostViewModel
                {
                    PostId = g.Key,
                    ContentPreview = g.First().Post.Content != null && g.First().Post.Content.Length > 100 ? g.First().Post.Content.Substring(0, 100) + "..." : g.First().Post.Content ?? "[No content]",
                    AuthorUserName = g.First().Post.ApplicationUser.UserName,
                    ReportReason = g.First().Reason,
                    ReporterUserName = g.First().ReportedByUser.UserName,
                    TotalReports = g.Count(),
                    ReportedAt = g.Min(r => r.CreatedAt)
                })
                .OrderByDescending(p => p.TotalReports)
                .ThenByDescending(p => p.ReportedAt)
                .ToListAsync();

            return reportedPosts;
        }

        public async Task<List<ReportedCommentViewModel>> GetReportedCommentsAsync()
        {
            // Query the database for reported comments
            var reportedComments = await _dbContext.CommentReports
                .GroupBy(r => r.CommentId)
                .Select(g => new ReportedCommentViewModel
                {
                    CommentId = g.Key,
                    ContentPreview = g.First().Comment.Content.Length > 100 
                        ? g.First().Comment.Content.Substring(0, 100) + "..." 
                        : g.First().Comment.Content,
                    AuthorUserName = g.First().Comment.Author.UserName,
                    ReportReason = g.First().Reason,
                    ReporterUserName = g.First().ReportedByUser.UserName,
                    TotalReports = g.Count(),
                    ReportedAt = g.Min(r => r.CreatedAt)
                })
                .OrderByDescending(r => r.TotalReports)
                .ThenByDescending(r => r.ReportedAt)
                .ToListAsync();

            return reportedComments;
        }

        public async Task<int> GetPendingPostReportsCountAsync()
        {
            // Count distinct posts with pending reports, excluding deleted posts
            return await _dbContext.PostReports
                .Where(r => r.Status == "Sent")
                .Where(r => r.Post != null && !r.Post.IsDeleted)
                .Select(r => r.PostId)
                .Distinct()
                .CountAsync();
        }

        public async Task<int> GetPendingCommentReportsCountAsync()
        {
            // Count distinct comments with pending reports, excluding deleted comments
            return await _dbContext.CommentReports
                .Where(r => r.Status == "Sent")
                .Where(r => r.Comment != null && !r.Comment.IsDeleted)
                .Select(r => r.CommentId)
                .Distinct()
                .CountAsync();
        }

        public async Task<bool> DeletePostAsync(Guid postId)
        {
            // Find the post
            var post = await _dbContext.Posts.FindAsync(postId);
            
            if (post == null)
            {
                throw new ArgumentException("Post not found", nameof(postId));
            }

            // Delete all reports for this post
            var reports = await _dbContext.PostReports.Where(r => r.PostId == postId).ToListAsync();
            _dbContext.PostReports.RemoveRange(reports);

            // Delete the post
            _dbContext.Posts.Remove(post);

            // Save changes
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId)
        {
            // Find the comment
            var comment = await _dbContext.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            // Delete associated reports first to maintain referential integrity
            var reports = await _dbContext.CommentReports.Where(r => r.CommentId == commentId).ToListAsync();
            _dbContext.CommentReports.RemoveRange(reports);

            // Delete the comment
            _dbContext.Comments.Remove(comment);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissPostReportAsync(Guid postId)
        {
            // Find the reports for this post
            var reports = await _dbContext.PostReports.Where(r => r.PostId == postId).ToListAsync();
            if (!reports.Any())
            {
                return false;
            }

            // Remove all reports for this post
            _dbContext.PostReports.RemoveRange(reports);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissCommentReportAsync(Guid commentId)
        {
            // Find the reports for this comment
            var reports = await _dbContext.CommentReports.Where(r => r.CommentId == commentId).ToListAsync();
            if (!reports.Any())
            {
                return false;
            }

            // Remove all reports for this comment
            _dbContext.CommentReports.RemoveRange(reports);
            
            // Save changes
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
