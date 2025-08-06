using ActioNator.Hubs;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Services.Interfaces.Community;
using ActioNator.ViewModels.Community;
using JWT.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CommunityController : Controller
    {
        private readonly ICommunityService _communityService;
        private readonly IHubContext<CommunityHub> _hubContext;
        private readonly ILogger<CommunityController> _logger;

        public CommunityController(
            ICommunityService communityService,
            IHubContext<CommunityHub> hubContext,
            ILogger<CommunityController> logger)
        {
            _communityService = communityService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                
                // Broadcast the post update to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceivePostUpdate", request.PostId, likesCount);

                return Json(new { success = true, likesCount });    
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling like for post {PostId}", request?.PostId);
                return StatusCode(500, new { success = false, message = "An error occurred while processing your request" });
            }
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
            
            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> GetPost(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest("Valid Post ID is required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var post = await _communityService.GetPostByIdAsync(id, userId);

            if (post == null)
            {
                return NotFound("Post not found");
            }

            return PartialView("_PostCardPartial", post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string content, List<IFormFile> images)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest(new { success = false, message = "Content is required" });
            }

            try
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userId = Guid.Parse(userIdString);
                
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
                
                // Create the post with images
                var post = await _communityService.CreatePostAsync(content, userId, images);
                
                // Broadcast the new post to all connected clients
                if (post != null)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveNewPost", post);
                }

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                return BadRequest("Valid Post ID is required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var likesCount = await _communityService.ToggleLikePostAsync(postId, userId);
            
            // Broadcast the post update to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceivePostUpdate", postId, likesCount);

            return Json(new { success = true, likesCount });
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
        public async Task<IActionResult> AddComment([FromBody] CommentRequest request)
        {
            if (request?.PostId == null || request.PostId == Guid.Empty || string.IsNullOrEmpty(request.Content))
            {
                return BadRequest("Valid Post ID and content are required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var comment = await _communityService.AddCommentAsync(request.PostId, request.Content, userId);
            
            // Broadcast the new comment to all connected clients
            if (comment != null)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNewComment", comment);
            }

            return Json(new { success = true, comment });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(Guid postId)
        {
            if (postId == Guid.Empty)
            {
                return BadRequest("Valid Post ID is required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var success = await _communityService.DeletePostAsync(postId, userId);
            
            // Broadcast the post deletion to all connected clients
            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceivePostDeletion", postId);
            }

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> DeleteComment(Guid commentId, [FromBody] CommentDeleteRequest request)
        {
            if (commentId == Guid.Empty)
            {
                return BadRequest("Valid Comment ID is required");
            }

            // Extract postId from the request
            var postId = request?.PostId ?? Guid.Empty;
            if (postId == Guid.Empty)
            {
                return BadRequest("Valid Post ID is required");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.Parse(userIdString);
            var success = await _communityService.DeleteCommentAsync(commentId, userId);
            
            // Broadcast the comment deletion to all connected clients
            if (success)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveCommentDeletion", commentId, postId);
            }

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost(Guid postId, string reason)
        {
            if (postId == Guid.Empty || string.IsNullOrEmpty(reason))
            {
                return BadRequest("Valid Post ID and reason are required");
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
            var userId = Guid.Parse(userIdString);
            var comment = await _communityService.GetCommentByIdAsync(id, userId);

            if (comment == null)
            {
                return NotFound("Comment not found");
            }

            return PartialView("_CommentItemPartial", comment);
        }
    }
}