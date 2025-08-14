using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Implementations.InputSanitization;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Interfaces.Communication;
using ActioNator.Services.Interfaces.Community;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

using static ActioNator.GCommon.FileConstants.ContentTypes;
using static ActioNator.GCommon.ValidationConstants.Comment;

namespace ActioNator.Services.Implementations.Community
{
    public class CommunityService : ICommunityService
    {
        private readonly ActioNatorDbContext _dbContext;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICloudinaryUrlService _cloudinaryUrlService;
        private readonly ISignalRService _signalRService;
        private readonly ILogger<CommunityService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInputSanitizationService _inputSanitizationService;

        public CommunityService(
            ActioNatorDbContext dbContext,
            ICloudinaryService cloudinaryService,
            ICloudinaryUrlService cloudinaryUrlService,
            ISignalRService signalRService,
            ILogger<CommunityService> logger,
            UserManager<ApplicationUser> userManager,
            IInputSanitizationService inputSanitizationService)
        {
            _dbContext = dbContext
                ?? throw new ArgumentNullException(nameof(dbContext));

            _cloudinaryService = cloudinaryService
                ?? throw new ArgumentNullException(nameof(cloudinaryService));

            _cloudinaryUrlService = cloudinaryUrlService
                ?? throw new ArgumentNullException(nameof(cloudinaryUrlService));

            _signalRService = signalRService
                ?? throw new ArgumentNullException(nameof(signalRService));

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));

            _userManager = userManager
                ?? throw new ArgumentNullException(nameof(userManager));

            _inputSanitizationService = inputSanitizationService
                ?? throw new ArgumentNullException(nameof(inputSanitizationService));
        }

        public async Task<IReadOnlyList<PostCardViewModel>> GetAllPostsAsync
        (
            Guid userId,
            string status = null,
            int pageNumber = 1,
            int pageSize = 20,
            bool isAdmin = false,
            CancellationToken cancellationToken = default
        )
        {
            if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(userId));

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            IQueryable<Post> query
                = _dbContext
                .Posts
                .AsNoTracking();

            // Authorization aware status handling
            if (!string.IsNullOrWhiteSpace(status))
            {
                string s = status
                    .Trim()
                    .ToLowerInvariant();

                if (s == "deleted")
                {
                    if (!isAdmin) // guard: only admins can request deleted posts
                        query = query.Where(p => p.IsPublic && !p.IsDeleted);
                    else
                        query = query.Where(p => p.IsDeleted);
                }
                else if (s == "active")
                {
                    query = query.Where(p => !p.IsDeleted && p.IsPublic);
                }
                // else "all" -> no filter (admin only recommended)
            }
            else
            {
                query = query.Where(p => p.IsPublic && !p.IsDeleted);
            }

            // Projection: produce the minimal shape we need in SQL
            var projected
                = query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Content,
                    p.CreatedAt,
                    p.UserId,
                    AuthorName = p.ApplicationUser!,
                    AuthorProfilePicture = p.ApplicationUser.ProfilePictureUrl,
                    LikeCount = p.LikesCount,
                    CommentCount = p
                        .Comments
                        .Count(c => !c.IsDeleted),
                    p.IsPublic,
                    p.IsDeleted,
                    // Images: pull URL + id (EF will perform a subquery)
                    Images = p
                        .PostImages
                        .Select(pi => new { pi.Id, pi.ImageUrl })
                        .ToList(),
                    // IsLiked by this user (translateable to SQL)
                    IsLiked = p
                        .Likes
                        .Any(l => l.UserId == userId && l.IsActive),
                });

            var rows
                = await
                projected
                .ToListAsync(cancellationToken);

            // Fetch comments for the selected posts (non-deleted), including author and likes info
            List<Guid> postIds = rows.Select(r => r.Id).ToList();

            var commentRows = await _dbContext
                .Comments
                .AsNoTracking()
                .Where(c => c.PostId != null
                            && postIds.Contains(c.PostId.Value)
                            && !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.Content,
                    c.CreatedAt,
                    c.AuthorId,
                    AuthorName = c.Author!.UserName,
                    AuthorProfilePicture = c.Author!.ProfilePictureUrl,
                    LikeCount = c.LikesCount,
                    IsDeleted = c.IsDeleted,
                    PostId = c.PostId,
                    IsLiked = c.Likes.Any(l => l.UserId == userId && l.IsActive)
                })
                .ToListAsync(cancellationToken);

            var commentsByPost = commentRows
                .OrderByDescending(c => c.CreatedAt)
                .GroupBy(c => c.PostId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(c => new PostCommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        AuthorId = c.AuthorId,
                        AuthorName = c.AuthorName!,
                        AuthorProfilePicture = c.AuthorProfilePicture!,
                        TimeAgo = GetTimeAgo(c.CreatedAt),
                        LikeCount = c.LikeCount,
                        IsAuthor = c.AuthorId == userId,
                        IsDeleted = false,
                        PostId = c.PostId!.Value,
                        IsLiked = c.IsLiked
                    }).ToList() as IEnumerable<PostCommentViewModel>
                );

            // Materialize into view models and attach comments
            List<PostCardViewModel> results
                = rows
                .Select(r => new PostCardViewModel
                {
                    Id = r.Id,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    AuthorId = r.UserId,
                    AuthorName = r.AuthorName.UserName!,
                    AuthorProfilePicture = r.AuthorProfilePicture,
                    LikeCount = r.LikeCount,
                    CommentCount = r.CommentCount,
                    IsLiked = r.IsLiked,
                    IsAuthor = r.UserId == userId,
                    IsPublic = r.IsPublic,
                    IsDeleted = r.IsDeleted,
                    Comments = commentsByPost.TryGetValue(r.Id, out var list)
                        ? list
                        : new List<PostCommentViewModel>(),
                    Images = r
                        .Images
                        .Select(i => new PostImageViewModel
                        {
                            Id = i.Id,
                            PostId = r.Id,
                            ImageUrl = i.ImageUrl,
                        }).ToList()
                }).ToList();

            return results;
        }

        public async Task<PostCardViewModel?> GetPostByIdAsync(Guid postId, Guid userId)
        {
            if (postId == Guid.Empty)
                throw new ArgumentException("Post ID cannot be empty.", nameof(postId));

            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            Post? post
                = await
                _dbContext
                .Posts
                .AsNoTracking()
                .Include(p => p.ApplicationUser)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .Include(p => p.PostImages)
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

            if (post == null)
                return null;

            return MapPostToViewModel(post, userId);
        }

        public async Task<PostCommentViewModel> GetCommentByIdAsync(Guid commentId, Guid userId)
        {
            if (commentId == Guid.Empty)
                throw new ArgumentException("Comment ID cannot be empty", nameof(commentId));

            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            Comment? comment =
                await
                _dbContext
                .Comments
                .AsNoTracking()
                .Include(c => c.Author)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

            if (comment is null)
                return null!; // Not found

            return MapCommentToViewModel(comment, userId)!;
        }

        public async Task<PostCardViewModel> CreatePostAsync
        (
            string content,
            Guid userId,
            CancellationToken
            cancellationToken = default,
            List<IFormFile>? images =null
        )
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content), "Post content cannot be empty.");

            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));

            images ??= new List<IFormFile>();

            ApplicationUser? user
                = await
                _dbContext
                .Users
                .FindAsync(userId)
                ?? throw new InvalidOperationException($"User with ID {userId} not found.");

            IDbContextTransaction? transaction
                = await
                _dbContext
                .Database
                .BeginTransactionAsync(cancellationToken);

            try
            {
                Post post = new()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Content = content,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    IsPublic = true
                };

                await _dbContext
                    .Posts
                    .AddAsync(post, cancellationToken);

                await _dbContext
                    .SaveChangesAsync(cancellationToken);

                IEnumerable<Task> uploadTasks
                    = images
                    .Where(img => img != null && img.Length > 0)
                    .Select(async image =>
                    {
                        try
                        {
                            // Optional: Add image validation here (file type, size)
                            if (IsSupported(image.ContentType)
                                && image.Length <= 10 * 1024 * 1024)
                            {
                                string imageUrl
                                    = await
                                    _cloudinaryService
                                    .UploadImageAsync(image, post.Id, "community", cancellationToken);

                                PostImage postImage = new()
                                {
                                    Id = Guid.NewGuid(),
                                    PostId = post.Id,
                                    ImageUrl = imageUrl,
                                };

                                await
                                _dbContext
                                .PostImages
                                .AddAsync(postImage, cancellationToken);
                            }
                            else
                            {
                                _logger
                                .LogError("Unsupported image type or size for post {PostId}", post.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger
                            .LogError(ex, "Failed to upload image for post {PostId}", post.Id);
                        }
                    });

                await Task
                    .WhenAll(uploadTasks);

                await _dbContext
                    .SaveChangesAsync(cancellationToken);

                await
                    _dbContext
                    .Entry(post)
                    .Reference(p => p.ApplicationUser)
                    .LoadAsync(cancellationToken);

                await
                    _dbContext
                    .Entry(post)
                    .Collection(p => p.PostImages)
                    .LoadAsync(cancellationToken);

                await transaction
                    .CommitAsync(cancellationToken);

                PostCardViewModel? postViewModel
                    = MapPostToViewModel(post, userId);

                await
                    _signalRService
                    .SendToAllAsync("ReceiveNewPost", postViewModel!);

                return postViewModel!;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating post for user {UserId}", userId);
                throw;
            }
        }

        public async Task<PostCommentViewModel> AddCommentAsync
        (Guid postId, 
            string content,
            Guid userId, 
            CancellationToken 
            cancellationToken =
            default
        )
        {
            if (postId == Guid.Empty)
                throw new ArgumentException("Post ID cannot be empty", nameof(postId));


            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentNullException(nameof(content), "Comment content cannot be empty or whitespace.");

            // Sanitize and validate content length
            string sanitized = _inputSanitizationService
                .SanitizeString(content)
                .Trim();

            if (string.IsNullOrWhiteSpace(sanitized))
                throw new ArgumentNullException(nameof(content), "Comment content cannot be empty after sanitization.");

            if (sanitized.Length < ContentMinLength || sanitized.Length > ContentMaxLength)
                throw new ArgumentOutOfRangeException(nameof(content), $"Comment must be between {ContentMinLength} and {ContentMaxLength} characters.");

            // Verify that the post exists and is not deleted
            Post? post
                = await
                _dbContext
                .Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId
                    && !p.IsDeleted, cancellationToken);

            if (post == null)
                throw new InvalidOperationException($"Post with ID {postId} not found or has been deleted.");

            // Verify that the user exists
            ApplicationUser? user
                = await
                _dbContext
                .Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                ?? throw new InvalidOperationException($"User with ID {userId} not found.");

            // Create the comment entity
            Comment comment = new Comment
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                AuthorId = userId,
                Content = sanitized,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            // Add the comment
            await _dbContext
                .Comments
                .AddAsync(comment, cancellationToken);
            await _dbContext
                .SaveChangesAsync(cancellationToken);

            // Load author navigation property for mapping (eager loading to avoid lazy-loading pitfalls)
            await _dbContext
                .Entry(comment)
                .Reference(c => c.Author)
                .LoadAsync(cancellationToken);

            // Map to view model
            PostCommentViewModel? commentViewModel
                = MapCommentToViewModel(comment, userId)
                ?? throw new InvalidOperationException("Failed to map comment to view model");

            // Broadcast the new comment to all connected clients
            await _signalRService
                .SendToAllAsync("ReceiveNewComment", commentViewModel);

            return commentViewModel;
        }

        public async Task<int> ToggleLikePostAsync
        (
            Guid postId, 
            Guid userId, 
            CancellationToken
            cancellationToken 
            = default
        )
        {
            if (postId == Guid.Empty)
                throw new ArgumentException("Post ID cannot be empty", nameof(postId));

            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            try
            {
                // Retrieve the post including its likes
                Post? post
                    = await
                    _dbContext
                    .Posts
                    .Include(p => p.Likes)
                    .FirstOrDefaultAsync(p => p.Id == postId
                        && !p.IsDeleted, cancellationToken)
                    ?? throw new InvalidOperationException($"Post with ID {postId} not found or deleted.");

                // Find active like by the user if exists
                Data.Models.PostLike? existingLike
                    = post
                    .Likes
                    .FirstOrDefault(l => l.UserId == userId && l.IsActive);

                if (existingLike != null)
                {
                    // Unlike post: deactivate existing like and decrement count safely
                    existingLike.IsActive = false;
                    post.LikesCount = Math.Max(0, post.LikesCount - 1);
                }
                else
                {
                    // Check for an inactive like to reactivate
                    Data.Models.PostLike? inactiveLike
                        = await
                        _dbContext
                        .PostLikes
                        .FirstOrDefaultAsync(l => l.PostId == postId
                            && l.UserId == userId
                            && !l.IsActive, cancellationToken);

                    if (inactiveLike != null)
                    {
                        inactiveLike.IsActive = true;
                    }
                    else
                    {
                        // Add new like
                        Data.Models.PostLike newLike
                            = new()
                            {
                                Id = Guid.NewGuid(),
                                PostId = postId,
                                UserId = userId,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            };

                        await
                            _dbContext
                            .PostLikes
                            .AddAsync(newLike, cancellationToken);
                    }

                    // Increment like count
                    post.LikesCount += 1;
                }

                // Save changes with cancellation support
                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                // Notify all connected clients about the like count update
                await
                    _signalRService
                    .SendToAllAsync("ReceivePostUpdate", postId, post.LikesCount);

                return post.LikesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<int> ToggleLikeCommentAsync
        (
            Guid commentId, 
            Guid userId, 
            CancellationToken 
            cancellationToken 
            = default
        )
        {
            if (commentId == Guid.Empty)
                throw new ArgumentException("Comment ID cannot be empty", nameof(commentId));

            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            try
            {
                // Retrieve the comment with its likes
                Comment? comment
                    = await
                    _dbContext
                    .Comments
                    .Include(c => c.Likes)
                    .FirstOrDefaultAsync(c => c.Id == commentId
                        && !c.IsDeleted, cancellationToken)
                    ?? throw new InvalidOperationException($"Comment with ID {commentId} not found or deleted.");

                // Find active like by the user if exists
                CommentLike? existingLike
                    = comment
                    .Likes
                    .FirstOrDefault(l => l.UserId == userId && l.IsActive);

                if (existingLike != null)
                {
                    // Unlike comment: deactivate the like and decrement count safely
                    existingLike.IsActive = false;
                    comment.LikesCount = Math.Max(0, comment.LikesCount - 1);
                }
                else
                {
                    // Check for an inactive like to reactivate
                    CommentLike? inactiveLike
                        = await
                        _dbContext
                        .CommentLikes
                        .FirstOrDefaultAsync(l => l.CommentId == commentId
                            && l.UserId == userId
                            && !l.IsActive, cancellationToken);

                    if (inactiveLike != null)
                    {
                        inactiveLike.IsActive = true;
                    }
                    else
                    {
                        // Add new like
                        CommentLike newLike
                            = new()
                            {
                                Id = Guid.NewGuid(),
                                CommentId = commentId,
                                UserId = userId,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            };

                        await
                            _dbContext
                            .CommentLikes
                            .AddAsync(newLike, cancellationToken);
                    }

                    // Increment like count
                    comment.LikesCount += 1;
                }

                // Save changes with cancellation support
                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                // Notify all clients about the update
                await
                    _signalRService
                    .SendToAllAsync("ReceiveCommentUpdate", commentId, comment.LikesCount);

                return comment.LikesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for comment {CommentId} by user {UserId}", commentId, userId);
                throw;
            }
        }

        public async Task<bool> DeletePostAsync
        (   
            Guid postId, 
            Guid userId, 
            CancellationToken 
            cancellationToken =
            default
        )
        {
            if (postId == Guid.Empty)
                throw new ArgumentException("Post ID cannot be empty", nameof(postId));
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID cannot be empty", nameof(userId));

            ApplicationUser user =
                await
                _dbContext
                .Users
                .FindAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException($"User with ID {userId} not found.");

            // Retrieve post including related images
            Post? post
                = await
                _dbContext
                .Posts
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

            if (post == null || post.IsDeleted)
            {
                _logger.LogWarning("Attempted to delete non-existent or already deleted post {PostId} by user {UserId}", postId, userId);
                return false;
            }

            // Authorization: only post author or admins are allowed
            if (post.UserId != userId)
            {
                bool isAdmin
                    = await
                    _userManager
                    .IsInRoleAsync(user, "Admin");

                if (!isAdmin)
                {
                    _logger.LogCritical(
                        "Unauthorized delete attempt for post {PostId} by user {UserId}",
                        postId, userId
                    );
                    return false; // Not authorized
                }

                // Admins can delete any post
                post.IsDeleted = true;
            }
            else
            {
                // Author deleting their own post
                post.IsDeleted = true;
            }

            // Collect public IDs from the post images or single ImageUrl
            List<string> publicIds = [];

            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                // Single image scenario
                string publicId
                    = _cloudinaryUrlService
                    .GetPublicId(post.ImageUrl);

                if (!string.IsNullOrEmpty(publicId))
                {
                    publicIds.Add(publicId);
                }
            }
            else if (post.PostImages != null && post.PostImages.Count != 0)
            {
                // Multiple images scenario
                publicIds = post
                    .PostImages
                    .Select(pi => _cloudinaryUrlService
                        .GetPublicId(pi.ImageUrl))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();
            }

            if (publicIds.Count > 0)
            {
                try
                {
                    await _cloudinaryService
                        .DeleteImagesByPublicIdsAsync(postId, publicIds, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger
                        .LogWarning(ex, "Failed to delete Cloudinary images for post {PostId}", postId);
                }
            }

            _dbContext
                .Posts
                .Update(post);

            await
                _dbContext
                .SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteCommentAsync
        (
            Guid commentId, 
            Guid userId, 
            CancellationToken 
            cancellationToken =
            default
        )
        {
            if (commentId == Guid.Empty) throw new ArgumentException(nameof(commentId));
            if (userId == Guid.Empty) throw new ArgumentException(nameof(userId));

            Comment? comment
                = await
                _dbContext
                .Comments
                .FindAsync(commentId, cancellationToken);

            if (comment == null || comment.IsDeleted)
            {
                _logger
                    .LogWarning("Attempted to delete non-existent or already deleted comment {CommentId} by user {UserId}", commentId, userId);
                return false; // Comment not found or already deleted
            }

            ApplicationUser user =
                await
                _dbContext
                .Users
                .FindAsync(userId, cancellationToken)
                ?? throw new InvalidOperationException($"User with ID {userId} not found.");

            bool isAuthorized = comment.AuthorId == userId
                || await _userManager.IsInRoleAsync(user, "Admin");

            if (!isAuthorized)
            {
                _logger
                    .LogCritical("Unauthorized delete attempt for comment {CommentId} by user {UserId}", commentId, userId);
                return false; // Not authorized
            }

            try
            {
                comment.IsDeleted = true;

                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                await
                    _signalRService
                    .SendToAllAsync("ReceiveCommentDeleted", comment.PostId.ToString()!, commentId);
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error deleting comment {CommentId}", commentId);
            }

            return true; // Successfully deleted
        }

        public async Task<bool> ReportPostAsync
        (
            Guid postId,
            string reason,
            Guid userId,
            CancellationToken 
            cancellationToken 
            = default,
            string details = ""
        )
        {
            if (postId == Guid.Empty) throw new ArgumentException("Post ID cannot be empty", nameof(postId));

            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason cannot be empty", nameof(reason));

            if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty", nameof(userId));

            // Optional: Validate reason length and sanitize here
            _inputSanitizationService
                .SanitizeString(reason);

            try
            {
                Post? post
                    = await
                    _dbContext
                    .Posts
                    .FindAsync(postId);

                if (post == null || post.IsDeleted)
                {
                    _logger
                        .LogCritical("Attempted to report non-existent or deleted post {PostId} by user {UserId}", postId, userId);

                    return false; // Post not found or already deleted
                }

                bool alreadyReported
                    = await
                    _dbContext
                    .PostReports
                    .AnyAsync(r => r.PostId == postId
                        && r.ReportedByUserId == userId
                        && r.Status == "Sent", cancellationToken);

                if (alreadyReported)
                {
                    _logger
                        .LogWarning("User {UserId} has already reported post {PostId}", userId, postId);
                    return false; // User has already reported this post
                }

                PostReport? report
                    = new()
                    {
                        Id = Guid.NewGuid(),
                        PostId = postId,
                        ReportedByUserId = userId,
                        Reason = reason,
                        Details = details ?? string.Empty,
                        CreatedAt = DateTime.UtcNow,
                        Status = "Pending" // Default status for new reports
                    };

                await
                    _dbContext
                    .PostReports
                    .AddAsync(report, cancellationToken);

                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Post {PostId} reported by user {UserId} for reason: {Reason}", postId, userId, reason);

                report
                    .Status = "Sent"; // Update status to indicate report has been sent

                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                await _signalRService.SendToGroupAsync("Admins", "ReceiveNewPostReport", report);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<bool> ReportCommentAsync
        (
            Guid commentId, 
            string reason, 
            Guid userId, 
            CancellationToken
            cancellationToken = default, 
            string details = ""
        )
        {
            if (commentId == Guid.Empty)
            {
                _logger
                    .LogError("Comment ID cannot be empty when reporting a comment.");
                throw new ArgumentException("Comment ID cannot be empty", nameof(commentId));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger
                    .LogError("Reason cannot be empty when reporting a comment.");

                throw new ArgumentException("Reason cannot be empty", nameof(reason));
            }

            if (userId == Guid.Empty)
            {
                _logger
                   .LogError("Reason cannot be empty when reporting a comment.");
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            try
            {
                // Find the comment
                Comment? comment
                    = await
                    _dbContext
                    .Comments
                    .FindAsync(commentId);

                if (comment == null || comment.IsDeleted)
                {
                    _logger
                        .LogWarning("Attempted to report non-existent or already deleted comment {CommentId} by user {UserId}", commentId, userId);
                    return false; // Comment not found or already deleted
                }

                if (comment.AuthorId == userId)
                {
                    _logger
                        .LogCritical("User {UserId} cannot report their own comment {CommentId}", userId, commentId);
                    return false; // User cannot report their own comment
                }

                // Check for existing pending report by the same user
                bool alreadyReported
                    = await
                    _dbContext
                    .CommentReports
                    .AnyAsync(r => r.CommentId == commentId
                                && r.ReportedByUserId == userId
                                && r.Status == "Sent");

                if (alreadyReported)
                {
                    _logger
                        .LogWarning("User {UserId} has already reported comment {CommentId}", userId, commentId);
                    return false; // Prevent duplicate pending reports
                }

                // Create the report
                CommentReport report = new()
                {
                    Id = Guid.NewGuid(),
                    CommentId = commentId,
                    ReportedByUserId = userId,
                    Reason = reason,
                    Details = details ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending",
                };

                await
                    _dbContext
                    .CommentReports
                    .AddAsync(report, cancellationToken);

                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Comment {CommentId} reported by user {UserId} for reason: {Reason}",
                    commentId, userId, reason);

                report
                    .Status = "Sent"; // Update status to indicate report has been sent

                await
                    _dbContext
                    .SaveChangesAsync(cancellationToken);

                // Send notification to admins
                await 
                    _signalRService
                    .SendToGroupAsync("Admins", "ReceiveNewCommentReport", report);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting comment {CommentId} by user {UserId}", commentId, userId);
                throw;
            }
        }

        #region Private Helper Methods
        private PostCardViewModel? MapPostToViewModel(Post post, Guid userId)
        {
            if (post == null) return null;

            List<Comment>? filteredComments 
                = post
                .Comments?
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return new PostCardViewModel
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                AuthorId = post.UserId,
                AuthorName = post.ApplicationUser.UserName!,
                AuthorProfilePicture 
                    = post.ApplicationUser?
                    .ProfilePictureUrl!,
                LikeCount = post.LikesCount,
                CommentCount = filteredComments?.Count ?? 0,
                IsLiked = post.Likes?
                    .Any(l => l.UserId == userId 
                        && l.IsActive) ?? false,
                IsAuthor = post.UserId == userId,
                IsPublic = post.IsPublic,
                IsDeleted = post.IsDeleted,
                Comments = filteredComments?
                    .Select(c => MapCommentToViewModel(c, userId))
                    .ToList()!,
                Images = post
                    .PostImages?
                    .Select(MapImageToViewModel)
                    .ToList()!,
            };
        }

        private PostImageViewModel MapImageToViewModel(PostImage image)
        {
            return new PostImageViewModel
            {
                Id = image.Id,
                PostId = image.PostId!.Value,
                ImageUrl = image.ImageUrl,
            };
        }

        private PostCommentViewModel? MapCommentToViewModel(Comment comment, Guid userId)
        {
            if (comment == null) return null;

            bool isLiked = comment
                .Likes?
                .Any(l => l.UserId == userId 
                    && l.IsActive) ?? false;

            return new PostCommentViewModel
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                AuthorId = comment.AuthorId,
                AuthorName = comment.Author?.UserName!,
                AuthorProfilePicture = comment.Author?.ProfilePictureUrl!,
                TimeAgo = GetTimeAgo(comment.CreatedAt),
                LikeCount = comment.LikesCount,
                IsAuthor = comment.AuthorId == userId,
                IsDeleted = comment.IsDeleted,
                PostId = comment.PostId ?? Guid.Empty,
                IsLiked = isLiked
            };
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            // Ensure UTC comparison to avoid time zone mismatches
            if (dateTime.Kind != DateTimeKind.Utc)
                dateTime = dateTime.ToUniversalTime();

            TimeSpan timeSpan = DateTime.UtcNow - dateTime;

            int minutes = (int)Math.Floor(timeSpan.TotalMinutes);
            int hours = (int)Math.Floor(timeSpan.TotalHours);
            int days = (int)Math.Floor(timeSpan.TotalDays);
            int months = days / 30;
            int years = days / 365;

            if (minutes < 1)
                return "Just now";
            if (minutes < 60)
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
            if (hours < 24)
                return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
            if (days < 30)
                return $"{days} day{(days == 1 ? "" : "s")} ago";
            if (days < 365)
                return $"{months} month{(months == 1 ? "" : "s")} ago";

            return $"{years} year{(years == 1 ? "" : "s")} ago";
        }

        #endregion
    }
}