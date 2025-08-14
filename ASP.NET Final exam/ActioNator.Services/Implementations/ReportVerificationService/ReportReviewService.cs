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

        public async Task<List<ReportedUserViewModel>> GetReportedUsersAsync()
        {
            // Query the database for reported users
            var reportedUsers = await _dbContext.UserReports
                .GroupBy(r => r.ReportedUserId)
                .Select(g => new ReportedUserViewModel
                {
                    UserId = g.Key,
                    UserName = g.First().ReportedUser.UserName,
                    ReportReason = g.First().Reason,
                    ReporterUserName = g.First().ReportedByUser.UserName,
                    TotalReports = g.Count(),
                    ReportedAt = g.Min(r => r.CreatedAt)
                })
                .OrderByDescending(u => u.TotalReports)
                .ThenByDescending(u => u.ReportedAt)
                .ToListAsync();

            return reportedUsers;
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
            // Find the post with comments for soft deletion
            var post = await _dbContext
                .Posts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                throw new ArgumentException("Post not found", nameof(postId));
            }

            // Remove all post reports
            var postReports = await _dbContext.PostReports
                .Where(r => r.PostId == postId)
                .ToListAsync();
            if (postReports.Any())
            {
                _dbContext.PostReports.RemoveRange(postReports);
            }

            // Soft-delete related comments and remove their reports
            if (post.Comments != null && post.Comments.Any())
            {
                var commentIds = post.Comments.Select(c => c.Id).ToList();

                var commentReports = await _dbContext.CommentReports
                    .Where(r => commentIds.Contains(r.CommentId))
                    .ToListAsync();
                if (commentReports.Any())
                {
                    _dbContext.CommentReports.RemoveRange(commentReports);
                }

                foreach (var c in post.Comments)
                {
                    c.IsDeleted = true;
                }
            }

            // Soft-delete the post
            post.IsDeleted = true;

            // Save changes
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId)
        {
            // Find the comment for soft deletion
            var comment = await _dbContext.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return false;
            }

            // Remove associated reports
            var reports = await _dbContext.CommentReports
                .Where(r => r.CommentId == commentId)
                .ToListAsync();
            if (reports.Any())
            {
                _dbContext.CommentReports.RemoveRange(reports);
            }

            // Soft-delete the comment
            comment.IsDeleted = true;
            
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

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            // Soft-delete the user and remove associated user reports
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            // Remove user reports for this user
            var reports = await _dbContext.UserReports.Where(r => r.ReportedUserId == userId).ToListAsync();
            if (reports.Any())
            {
                _dbContext.UserReports.RemoveRange(reports);
            }

            // Soft-delete user
            user.IsDeleted = true;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissUserReportAsync(Guid userId)
        {
            // Remove all reports for this user without deleting the user
            var reports = await _dbContext.UserReports.Where(r => r.ReportedUserId == userId).ToListAsync();
            if (!reports.Any())
            {
                return false;
            }

            _dbContext.UserReports.RemoveRange(reports);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
