using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.UserDashboard;
using ActioNator.ViewModels.Community;
using ActioNator.ViewModels.Dashboard;
using ActioNator.Services.Interfaces.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActioNator.Areas.User.Controllers
{
    [Authorize]
    [Area("User")]
    public class HomeController : BaseController
    {
        private readonly IUserDashboardService _dashboardService;
        private readonly ILogger<HomeController> _logger;
        private readonly ICommunityService _communityService;

        // Pass UserManager to base constructor
        public HomeController(
            IUserDashboardService dashboardService,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger,
            ICommunityService communityService)
            : base(userManager)
        {
            _dashboardService = dashboardService
                ?? throw new ArgumentNullException(nameof(dashboardService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
            _communityService = communityService
                ?? throw new ArgumentNullException(nameof(communityService));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                Guid? userId = GetUserId();

                if (!userId.HasValue)
                {
                    _logger
                        .LogCritical("Failed to get user ID from claims.");
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                ApplicationUser? user
                    = await GetUserAsync(userId.Value);

                if (user == null)
                {
                    _logger
                        .LogCritical("User with ID {UserId} not found.", userId.Value);
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                DashboardViewModel dashboardViewModel
                    = await
                    _dashboardService
                    .GetDashboardDataAsync(userId.Value, user);

                // Fetch the latest 3 posts using the Community service to ensure
                // identical data mapping/behavior as the Community page
                var recentCommunityPosts = await _communityService.GetAllPostsAsync(
                    userId.Value,
                    status: null,
                    pageNumber: 1,
                    pageSize: 3,
                    isAdmin: User.IsInRole("Administrator"));

                dashboardViewModel.ConvertedRecentPosts = recentCommunityPosts;

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                Guid? currentUserId = GetUserId();

                _logger
                    .LogError(ex, "Error occurred while loading dashboard for user {UserId}", currentUserId);
                return View("Error");
            }
        }

        private PostCardViewModel ConvertToCommunityPostCardViewModel(ViewModels.Posts.PostCardViewModel post)
            => new PostCardViewModel
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                AuthorName = post.AuthorName,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                IsLiked = false,
                LikesCount = post.LikesCount,
                CommentsCount = post.CommentsCount,
                SharesCount = post.SharesCount,
                IsDeleted = post.IsDeleted,
                ProfilePictureUrl = post.ProfilePictureUrl,
                Images = post
                .Images?
                .Select(img => new PostImageViewModel
                {
                    Id = img.Id,
                    PostId = img.PostId ?? Guid.Empty,
                    ImageUrl = img.ImageUrl,
                }).ToList() ?? [],
                Comments = post.Comments?.Select(c => new PostCommentViewModel
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorId = c.AuthorId,
                    AuthorName = c.AuthorName,
                    CreatedAt = c.CreatedAt,
                    ProfilePictureUrl = c.ProfilePictureUrl,
                    IsDeleted = c.IsDeleted
                }).ToList() ?? []
            };
    }
}
