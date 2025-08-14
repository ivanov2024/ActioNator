using ActioNator.Hubs;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Services.Interfaces.Community;
using ActioNator.ViewModels.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ActioNator.Data.Models;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CommunityController : Controller
    {
        private readonly ICommunityService _communityService;
        private readonly IHubContext<CommunityHub> _hubContext;
        private readonly ILogger<CommunityController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommunityController(
            ICommunityService communityService,
            IHubContext<CommunityHub> hubContext,
            ILogger<CommunityController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _communityService = communityService;
            _hubContext = hubContext;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string status = null)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            
            // Only allow status filtering for admin users
            bool isAdmin = User.IsInRole("Admin");
            string statusFilter = isAdmin ? status : null;
            
            var posts = await _communityService.GetAllPostsAsync(userId, statusFilter);
            
            // Pass isAdmin to the view to control visibility of the filter
            ViewBag.IsAdmin = isAdmin;
            ViewBag.CurrentStatus = status ?? "all";
            
             // Pass current user's profile picture URL for avatar display in Create Post UI
            var currentUser = await _userManager.FindByIdAsync(userIdString);
            ViewBag.CurrentUserProfilePictureUrl = currentUser?.ProfilePictureUrl;
            
            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> GetPost(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest("Valid Post ID is required");
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var userId = Guid.Parse(userIdString);
                var post = await _communityService.GetPostByIdAsync(id, userId);

                if (post == null)
                {
                    return NotFound("Post not found");
                }

                // The service now returns ActioNator.ViewModels.Community.PostCardViewModel directly
                // No need for conversion
                return PartialView("_PostCardPartial", post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting post {PostId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost([FromForm] string content, [FromForm] List<IFormFile> images)
        {
            // Allow posts with text and/or images
            if (string.IsNullOrWhiteSpace(content) && (images == null || images.Count == 0))
            {
                return BadRequest(new { success = false, message = "Please provide text or at least one image." });
            }

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userId = Guid.Parse(userIdString);
                _logger.LogInformation("CreatePost received {Count} images", images?.Count ?? 0);
                
                // Validate images before sending to service
                if (images != null && images.Count > 0)
                {
                    // Check if any image exceeds the maximum allowed size (5MB)
                    const int maxFileSizeBytes = 5 * 1024 * 1024;
                    var oversizedImages = images.Where(img => img != null && img.Length > maxFileSizeBytes).ToList();
                    if (oversizedImages.Any())
                    {
                        return BadRequest(new { success = false, message = $"One or more images exceed the maximum allowed size of 5MB" });
                    }
                    
                    // Check if any image has an invalid type
                    string[] allowedTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    var invalidImages = images.Where(img => img != null && !allowedTypes.Contains(img.ContentType.ToLower())).ToList();
                    if (invalidImages.Any())
                    {
                        return BadRequest(new { success = false, message = $"One or more images have an invalid format. Allowed formats: JPG, PNG, GIF, WEBP" });
                    }
                }
                
                CancellationToken cancelationToken = new();

                // Create the post with images
                PostCardViewModel post 
                    = await _communityService
                    .CreatePostAsync(content, userId, cancelationToken,  images);
                
                // Service already broadcasts the new post via SignalR; no need to broadcast here again
                return Json(new { success = true, postId = post?.Id, message = "Post created successfully" });
            }
            catch (ArgumentException ex)
            {
                // Handle validation errors
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error creating post");
                return StatusCode(500, new { success = false, message = "An error occurred while creating your post. Please try again." });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> ToggleLikeComment([FromBody] CommentLikeRequest request)
        {
            if (request?.CommentId == null || request.CommentId == Guid.Empty)
            {
                return BadRequest("Valid Comment ID is required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var likesCount = await _communityService.ToggleLikeCommentAsync(request.CommentId, userId);
            
            // Broadcast the comment update to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceiveCommentUpdate", request.CommentId, likesCount);

            return Json(new { success = true, likesCount });
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> ToggleLike([FromBody] PostLikeRequest request)
        {
            try
            {
                if (request?.PostId == null || request.PostId == Guid.Empty)
                {
                    return BadRequest(new { success = false, message = "Valid Post ID is required" });
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var userId = Guid.Parse(userIdString);
                var likesCount = await _communityService.ToggleLikePostAsync(request.PostId, userId);

                await _hubContext.Clients.All.SendAsync("ReceivePostUpdate", request.PostId, likesCount);

                return Json(new { success = true, likesCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId}", request?.PostId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> AddComment([FromBody] CommentRequest request)
        {
            try
            {
                if (request?.PostId == null || request.PostId == Guid.Empty || string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { success = false, message = "Valid Post ID and non-empty content are required" });
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var userId = Guid.Parse(userIdString);
                var comment = await _communityService.AddCommentAsync(request.PostId, request.Content, userId);

                // No explicit SignalR broadcast here; service handles broadcasting
                return Json(new { success = true, comment });
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogWarning(ex, "Invalid add comment payload for post {PostId}", request?.PostId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogWarning(ex, "Comment length out of range for post {PostId}", request?.PostId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid add comment request for post {PostId}", request?.PostId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operation failed while adding comment for post {PostId}", request?.PostId);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding comment for post {PostId}", request?.PostId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { success = false, message = "Valid Post ID is required" });
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var userId = Guid.Parse(userIdString);
                var success = await _communityService.DeletePostAsync(id, userId);
                
                // Broadcast the post deletion to all connected clients
                if (success)
                {
                    await _hubContext.Clients.All.SendAsync("ReceivePostDeletion", id);
                }

                return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> DeleteComment(Guid commentId, [FromBody] CommentDeleteRequest request, [FromRoute(Name = "id")] Guid? routeId = null)
        {
            try
            {
                // Support both /.../DeleteComment/{commentId} (old) and conventional {id?} route
                if (commentId == Guid.Empty && routeId.HasValue)
                {
                    commentId = routeId.Value;
                }

                if (commentId == Guid.Empty)
                {
                    return BadRequest(new { success = false, message = "Valid Comment ID is required" });
                }

                // Extract postId from the request
                var postId = request?.PostId ?? Guid.Empty;
                if (postId == Guid.Empty)
                {
                    return BadRequest(new { success = false, message = "Valid Post ID is required" });
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }
                
                var userId = Guid.Parse(userIdString);
                var success = await _communityService.DeleteCommentAsync(commentId, userId);
            
            // Broadcast the comment deletion to all connected clients
            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveCommentDeletion", commentId, postId);
            }

            return Json(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> ReportPost(Guid postId, string reason, [FromRoute(Name = "id")] Guid? routeId = null)
        {
            // Support both /.../ReportPost/{postId} (named parameter) and conventional {id?} route
            if (postId == Guid.Empty && routeId.HasValue)
            {
                postId = routeId.Value;
            }

            if (postId == Guid.Empty)
            {
                return BadRequest("Valid Post ID is required");
            }

            if (string.IsNullOrEmpty(reason))
            {
                reason = "Inappropriate content"; // Default reason if none provided
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var success = await _communityService.ReportPostAsync(postId, reason, userId);

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> ReportComment(Guid commentId, [FromBody] CommentReportRequest request)
        {
            if (commentId == Guid.Empty)
            {
                return BadRequest("Valid Comment ID is required");
            }

            // Extract reason from the request
            var reason = request?.Reason;
            if (string.IsNullOrEmpty(reason))
            {
                reason = "Inappropriate content"; // Default reason if none provided
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var success = await _communityService.ReportCommentAsync(commentId, reason, userId);

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> ReportUser(Guid reportedUserId, [FromBody] UserReportRequest request, [FromRoute(Name = "id")] Guid? routeId = null)
        {
            // Support conventional areas route that uses {id?}
            if (reportedUserId == Guid.Empty && routeId.HasValue)
            {
                reportedUserId = routeId.Value;
            }

            if (reportedUserId == Guid.Empty)
            {
                return BadRequest("Valid User ID is required");
            }

            var reason = request?.Reason;
            if (string.IsNullOrEmpty(reason))
            {
                reason = "Inappropriate content"; // Default reason if none provided
            }

            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserIdString))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }

            var currentUserId = Guid.Parse(currentUserIdString);
            var success = await _communityService.ReportUserAsync(reportedUserId, reason, currentUserId);

            if (!success)
            {
                // Common reasons: already reported by this user, self-report attempt, or target user deleted/non-existent
                return Json(new { success = false, message = "You have already reported this user or you are not allowed to report this user." });
            }

            return Json(new { success = true });
        }

        // TODO: Add methods for image upload functionality
        
        /// <summary>
        /// Gets a comment by ID for real-time updates
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetComment(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Valid Comment ID is required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Unauthorized(new { success = false, message = "User not authenticated" });
            }
            var userId = Guid.Parse(userIdString);

            var comment = await _communityService.GetCommentByIdAsync(id, userId);
            if (comment == null)
            {
                return NotFound("Comment not found");
            }

            // If mapping is needed, do it here (assume comment is already a PostCommentViewModel)
            return PartialView("~/Views/Shared/_CommentItemPartial.cshtml", comment);
        }
    }
}