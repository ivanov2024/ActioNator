using ActioNator.Services.Interfaces.Community;
using ActioNator.ViewModels.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class CommunityController : Controller
    {
        private readonly ICommunityService _communityService;

        public CommunityController(ICommunityService communityService)
        {
            _communityService = communityService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var posts = await _communityService.GetAllPostsAsync(userId);
            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> GetPost(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Post ID is required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var post = await _communityService.GetPostByIdAsync(id, userId);

            if (post == null)
            {
                return NotFound("Post not found");
            }

            return PartialView("_PostCardPartial", post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return BadRequest("Content is required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var postId = await _communityService.CreatePostAsync(content, userId);

            return Json(new { success = true, postId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(string postId)
        {
            if (string.IsNullOrEmpty(postId))
            {
                return BadRequest("Post ID is required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var likesCount = await _communityService.ToggleLikePostAsync(postId, userId);

            return Json(new { success = true, likesCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(string postId, string content)
        {
            if (string.IsNullOrEmpty(postId) || string.IsNullOrEmpty(content))
            {
                return BadRequest("Post ID and content are required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var comment = await _communityService.AddCommentAsync(postId, content, userId);

            return PartialView("_CommentItemPartial", comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(string postId)
        {
            if (string.IsNullOrEmpty(postId))
            {
                return BadRequest("Post ID is required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _communityService.DeletePostAsync(postId, userId);

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(string commentId)
        {
            if (string.IsNullOrEmpty(commentId))
            {
                return BadRequest("Comment ID is required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _communityService.DeleteCommentAsync(commentId, userId);

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportPost(string postId, string reason)
        {
            if (string.IsNullOrEmpty(postId) || string.IsNullOrEmpty(reason))
            {
                return BadRequest("Post ID and reason are required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _communityService.ReportPostAsync(postId, reason, userId);

            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportComment(string commentId, string reason)
        {
            if (string.IsNullOrEmpty(commentId) || string.IsNullOrEmpty(reason))
            {
                return BadRequest("Comment ID and reason are required");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var success = await _communityService.ReportCommentAsync(commentId, reason, userId);

            return Json(new { success });
        }

        // TODO: Add methods for image upload functionality
    }
}