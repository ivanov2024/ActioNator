using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Interfaces.Communication;
using ActioNator.Services.Interfaces.Community;
using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ActioNator.Services.Implementations.Community
{
    public class CommunityService : ICommunityService
    {
        private readonly ActioNatorDbContext _dbContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<CommunityService> _logger;
        
        public CommunityService(
            ActioNatorDbContext dbContext,
            ICloudinaryService cloudinaryService,
            ISignalRService signalRService,
            ILogger<CommunityService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _cloudinaryService = cloudinaryService ?? throw new ArgumentNullException(nameof(cloudinaryService));
            _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<PostCardViewModel>> GetAllPostsAsync(Guid userId, string status = null)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Get all posts with their authors, comments, images, and likes
            var query = _dbContext
                .Posts
                .AsNoTracking()
                .Include(p => p.ApplicationUser)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .Include(p => p.PostImages)
                .Include(p => p.Likes)
                .AsQueryable();
                
            // Apply status filter if provided
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "deleted":
                        query = query.Where(p => p.IsDeleted);
                        break;
                    case "active":
                        query = query.Where(p => !p.IsDeleted && p.IsPublic);
                        break;
                    // "all" or default case - no additional filtering
                }
            }
            else
            {
                // Default behavior (non-admin users) - only show public, non-deleted posts
                query = query.Where(p => p.IsPublic && !p.IsDeleted);
            }
            
            // Get the filtered posts
            IEnumerable<Post>? posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Map to view models
            var postViewModels 
                = posts
                .Select(p => MapPostToViewModel(p, userId)).ToList();

            return postViewModels;
        }

        /// <summary>
        /// Maps a Post entity to a PostCardViewModel
        /// </summary>
        private PostCardViewModel? MapPostToViewModel(Post post, Guid userId)
        {
            if (post == null) return null;
            
            // Check if the current user has liked this post
            bool isLiked = post.Likes != null && 
                          post.Likes.Any(l => l.UserId == userId && l.IsActive);

            return new PostCardViewModel
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                AuthorId = post.UserId,
                AuthorName = post.ApplicationUser?.UserName ?? "Unknown",
                AuthorProfilePicture = post.ApplicationUser?.ProfilePictureUrl ?? "/img/default-profile.png",
                LikeCount = post.LikesCount,
                CommentCount = post.Comments?.Count(c => !c.IsDeleted) ?? 0,
                IsLiked = isLiked,
                IsAuthor = post.UserId == userId,
                IsPublic = post.IsPublic,
                IsDeleted = post.IsDeleted,
                Comments 
                = post.Comments?
                    .Where(c => !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => MapCommentToViewModel(c, userId))
                    .ToList(),
                Images = post.PostImages?
                    .Select(i => new PostImageViewModel
                    {
                        Id = i.Id,
                        PostId = i.PostId ?? Guid.Empty,
                        ImageUrl = i.ImageUrl,
                        CreatedAt = DateTime.UtcNow // Default value since PostImage doesn't have CreatedAt
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Maps a Comment entity to a PostCommentViewModel
        /// </summary>
        private PostCommentViewModel? MapCommentToViewModel(Comment comment, Guid userId)
        {
            if (comment == null) return null;

            // Check if the current user has liked this comment
            bool isLiked = comment.Likes != null && 
                          comment.Likes.Any(l => l.UserId == userId && l.IsActive);

            return new PostCommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                AuthorId = comment.AuthorId,
                AuthorName = comment.Author.UserName,
                AuthorProfilePicture = comment.Author.ProfilePictureUrl ?? "/images/default-profile.png",
                TimeAgo = GetTimeAgo(comment.CreatedAt),
                LikeCount = comment.LikesCount,
                IsAuthor = comment.AuthorId == userId,
                IsDeleted = comment.IsDeleted,
                PostId = comment.PostId ?? Guid.Empty,
                IsLiked = isLiked
            };
        }

        /// <summary>
        /// Calculates a human-readable time ago string
        /// </summary>
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes == 1 ? "" : "s")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays == 1 ? "" : "s")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{((int)(timeSpan.TotalDays / 30) == 1 ? "" : "s")} ago";

            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) == 1 ? "" : "s")} ago";
        }

        public async Task<PostCardViewModel> GetPostByIdAsync(Guid postId, Guid userId)
        {
            if (postId == Guid.Empty)
            {
                throw new ArgumentException("Post ID cannot be empty", nameof(postId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Get the post with its author, comments, images, and likes
            var post = await _dbContext.Posts
                .Include(p => p.ApplicationUser)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .Include(p => p.PostImages)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null)
            {
                return null; // Post not found
            }

            // Map to view model
            return MapPostToViewModel(post, userId);
        }

        public async Task<PostCommentViewModel> GetCommentByIdAsync(Guid commentId, Guid userId)
        {
            if (commentId == Guid.Empty)
            {
                throw new ArgumentException("Comment ID cannot be empty", nameof(commentId));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Get the comment with its author and likes
            var comment = await _dbContext.Comments
                .Include(c => c.Author)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment == null)
            {
                return null; // Comment not found
            }

            // Map to view model
            return MapCommentToViewModel(comment, userId);
        }

        public async Task<PostCardViewModel> CreatePostAsync(string content, Guid userId)
        {
            // Call the overloaded method with empty images list
            return await CreatePostAsync(content, userId, new List<IFormFile>());
        }
        
        public async Task<PostCardViewModel> CreatePostAsync(string content, Guid userId, List<IFormFile> images)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Get the user
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            // Create the post
            var post = new Post
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false,
                IsPublic = true // Default to public
            };

            // Save to database
            await _dbContext.Posts.AddAsync(post);
            await _dbContext.SaveChangesAsync();
            
            // Process and upload images if any
            if (images != null && images.Count > 0)
            {
                foreach (var image in images)
                {
                    if (image != null && image.Length > 0)
                    {
                        try
                        {
                            // Upload image to Cloudinary
                            string imageUrl = await _cloudinaryService.UploadImageAsync(image,post.Id,  "community");
                            
                            // Create post image entity
                            var postImage = new PostImage
                            {
                                Id = Guid.NewGuid(),
                                PostId = post.Id,
                                ImageUrl = imageUrl,
                            };
                            
                            // Save post image to database
                            await _dbContext
                                .PostImages
                                .AddAsync(postImage);
                        }
                        catch (Exception ex)
                        {
                            // Log the error but continue processing
                            Console.WriteLine($"Error uploading image: {ex.Message}");
                        }
                    }
                }
                
                // Save all post images
                await _dbContext.SaveChangesAsync();
            }

            // Load the user and images for mapping
            await _dbContext.Entry(post).Reference(p => p.ApplicationUser).LoadAsync();
            await _dbContext.Entry(post).Collection(p => p.PostImages).LoadAsync();

            // Map to view model
            var postViewModel = MapPostToViewModel(post, userId);

            await _signalRService.SendToAllAsync("ReceiveNewPost", postViewModel);

            return postViewModel;
        }

        public async Task<PostCommentViewModel> AddCommentAsync(Guid postId, string content, Guid userId)
        {
            if (postId == Guid.Empty)
            {
                throw new ArgumentException("Post ID cannot be empty", nameof(postId));
            }

            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            // Get the post
            var post = await _dbContext.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
            {
                throw new InvalidOperationException($"Post with ID {postId} not found");
            }

            // Get the user
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException($"User with ID {userId} not found");
            }

            // Create the comment
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                AuthorId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Save to database
            await _dbContext.Comments.AddAsync(comment);
            await _dbContext.SaveChangesAsync();

            // Load the author for mapping
            await _dbContext.Entry(comment).Reference(c => c.Author).LoadAsync();

            // Map to view model
            var commentViewModel = MapCommentToViewModel(comment, userId);
            
            if (commentViewModel == null)
            {
                throw new InvalidOperationException("Failed to map comment to view model");
            }

            // Broadcast the new comment to all connected clients
            await _signalRService.SendToAllAsync("ReceiveNewComment", commentViewModel);

            return commentViewModel;
        }

        public async Task<int> ToggleLikePostAsync(Guid postId, Guid userId)
        {
            if (postId == Guid.Empty || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    postId == Guid.Empty ? nameof(postId) : nameof(userId));
            }

            try
            {
                // Find the post with eager loading of likes
                var post = await _dbContext.Posts
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

                if (post == null)
                {
                    throw new InvalidOperationException($"Post with ID {postId} not found");
                }

                // Check if the user already liked this post
                var existingLike = post.Likes.FirstOrDefault(l => l.UserId == userId && l.IsActive);
                bool isLiked;

                if (existingLike != null)
                {
                    // User already liked the post, so unlike it
                    existingLike.IsActive = false;
                    isLiked = false;
                    post.LikesCount = Math.Max(0, post.LikesCount - 1);
                }
                else
                {
                    // Check if there's an inactive like to reactivate
                    var inactiveLike = await _dbContext.PostLikes
                        .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId && !l.IsActive);

                    if (inactiveLike != null)
                    {
                        // Reactivate the like
                        inactiveLike.IsActive = true;
                        isLiked = true;
                    }
                    else
                    {
                        // Create a new like
                        var newLike = new ActioNator.Data.Models.PostLike
                        {
                            Id = Guid.NewGuid(),
                            PostId = postId,
                            UserId = userId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _dbContext.PostLikes.AddAsync(newLike);
                        isLiked = true;
                    }

                    // Increment the like count
                    post.LikesCount += 1;
                }

                _dbContext.Update(post);
                await _dbContext.SaveChangesAsync();

                // Broadcast the update to all connected clients
                await _signalRService.SendToAllAsync("ReceivePostUpdate", postId, post.LikesCount);

                return post.LikesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }
        
        public async Task<int> ToggleLikeCommentAsync(Guid commentId, Guid userId)
        {
            if (commentId == Guid.Empty || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    commentId == Guid.Empty ? nameof(commentId) : nameof(userId));
            }

            try
            {
                // Find the comment with eager loading of likes
                var comment = await _dbContext.Comments
                    .Include(c => c.Likes)
                    .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

                if (comment == null)
                {
                    throw new InvalidOperationException($"Comment with ID {commentId} not found");
                }

                // Check if the user already liked this comment
                var existingLike = comment.Likes.FirstOrDefault(l => l.UserId == userId && l.IsActive);
                bool isLiked;

                if (existingLike != null)
                {
                    // User already liked the comment, so unlike it
                    existingLike.IsActive = false;
                    isLiked = false;
                    comment.LikesCount = Math.Max(0, comment.LikesCount - 1);
                }
                else
                {
                    // Check if there's an inactive like to reactivate
                    var inactiveLike = await _dbContext.CommentLikes
                        .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId && !l.IsActive);

                    if (inactiveLike != null)
                    {
                        // Reactivate the like
                        inactiveLike.IsActive = true;
                        isLiked = true;
                    }
                    else
                    {
                        // Create a new like
                        var newLike = new ActioNator.Data.Models.CommentLike
                        {
                            Id = Guid.NewGuid(),
                            CommentId = commentId,
                            UserId = userId,
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _dbContext.CommentLikes.AddAsync(newLike);
                        isLiked = true;
                    }

                    // Increment the like count
                    comment.LikesCount += 1;
                }

                _dbContext.Update(comment);
                await _dbContext.SaveChangesAsync();

                // Broadcast the update to all connected clients
                await _signalRService.SendToAllAsync("ReceiveCommentUpdate", commentId, comment.LikesCount);

                return comment.LikesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for comment {CommentId} by user {UserId}", commentId, userId);
                throw;
            }
        }

        public async Task<bool> DeletePostAsync(Guid postId, Guid userId)
        {
            if (postId == Guid.Empty || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    postId == Guid.Empty ? nameof(postId) : nameof(userId));
            }

            // Find the post with its images
            var post = await _dbContext.Posts
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.Id == postId);
                
            if (post == null || post.IsDeleted)
            {
                return false; // Post not found or already deleted
            }

            // Check if user is the author
            if (post.UserId != userId)
            {
                // Admin check removed temporarily
                return false; // Not authorized
            }
            
            // Delete associated images from Cloudinary
            if (post.PostImages != null && post.PostImages.Any())
            {
                foreach (var postImage in post.PostImages)
                {
                    try
                    {
                        // Extract public ID from the Cloudinary URL
                        string publicId = _cloudinaryService.GetPublicIdFromUrl(postImage.ImageUrl);
                        
                        if (!string.IsNullOrEmpty(publicId))
                        {
                            // Delete the image from Cloudinary
                            await _cloudinaryService.DeleteImageAsync(publicId);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue processing
                        Console.WriteLine($"Error deleting image from Cloudinary: {ex.Message}");
                    }
                }
            }

            // Soft delete the post
            post.IsDeleted = true;
            _dbContext.Posts.Update(post);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId)
        {
            if (commentId == Guid.Empty || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    commentId == Guid.Empty ? nameof(commentId) : nameof(userId));
            }

            // Find the comment
            var comment = await _dbContext.Comments.FindAsync(commentId);
            if (comment == null || comment.IsDeleted)
            {
                return false; // Comment not found or already deleted
            }

            // Check if user is the author
            if (comment.AuthorId != userId)
            {
                // Admin check removed temporarily
                return false; // Not authorized
            }

            // Soft delete the comment
            comment.IsDeleted = true;
            _dbContext.Comments.Update(comment);
            await _dbContext.SaveChangesAsync();

            await _signalRService.SendToAllAsync("ReceiveCommentDeleted", comment.PostId.ToString(), commentId);
            return true;
        }

        public async Task<bool> ReportPostAsync(Guid postId, string reason, Guid userId)
        {
            if (postId == Guid.Empty || string.IsNullOrEmpty(reason) || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    postId == Guid.Empty ? nameof(postId) : 
                    reason == null ? nameof(reason) : nameof(userId));
            }

            try
            {
                // Find the post
                var post = await _dbContext.Posts.FindAsync(postId);
                if (post == null || post.IsDeleted)
                {
                    return false; // Post not found or already deleted
                }

                // Create the report
                var report = new PostReport
                {
                    Id = Guid.NewGuid(),
                    PostId = postId,
                    ReportedByUserId = userId,
                    Reason = reason,
                    Details = string.Empty, // Could be expanded to accept details from UI
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                // Save to database
                await _dbContext.PostReports.AddAsync(report);
                await _dbContext.SaveChangesAsync();

                // Log the report
                _logger.LogInformation("Post {PostId} reported by user {UserId} for reason: {Reason}", 
                    postId, userId, reason);

                // Notify admins via SignalR (if admin hub group is set up)
                await _signalRService.SendToGroupAsync("Admins", "ReceiveNewPostReport", new
                {
                    ReportId = report.Id,
                    PostId = report.PostId,
                    ReportedBy = report.ReportedByUserId,
                    Reason = report.Reason,
                    CreatedAt = report.CreatedAt
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<bool> ReportCommentAsync(Guid commentId, string reason, Guid userId)
        {
            if (commentId == Guid.Empty || string.IsNullOrEmpty(reason) || userId == Guid.Empty)
            {
                throw new ArgumentException(
                    commentId == Guid.Empty ? nameof(commentId) : 
                    reason == null ? nameof(reason) : nameof(userId));
            }

            try
            {
                // Find the comment
                var comment = await _dbContext.Comments.FindAsync(commentId);
                if (comment == null || comment.IsDeleted)
                {
                    return false; // Comment not found or already deleted
                }

                // Create the report
                var report = new CommentReport
                {
                    Id = Guid.NewGuid(),
                    CommentId = commentId,
                    ReportedByUserId = userId,
                    Reason = reason,
                    Details = string.Empty, // Could be expanded to accept details from UI
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                // Save to database
                await _dbContext.CommentReports.AddAsync(report);
                await _dbContext.SaveChangesAsync();

                // Log the report
                _logger.LogInformation("Comment {CommentId} reported by user {UserId} for reason: {Reason}", 
                    commentId, userId, reason);

                // Notify admins via SignalR (if admin hub group is set up)
                await _signalRService.SendToGroupAsync("Admins", "ReceiveNewCommentReport", new
                {
                    ReportId = report.Id,
                    CommentId = report.CommentId,
                    ReportedBy = report.ReportedByUserId,
                    Reason = report.Reason,
                    CreatedAt = report.CreatedAt
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting comment {CommentId} by user {UserId}", commentId, userId);
                throw;
            }
        }
    }
}