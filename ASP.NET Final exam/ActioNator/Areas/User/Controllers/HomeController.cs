using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.UserDashboard;
using ActioNator.ViewModels.Dashboard;
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

        // Pass UserManager to base constructor
        public HomeController(
            IUserDashboardService dashboardService,
            UserManager<ApplicationUser> userManager,
            ILogger<HomeController> logger)
            : base(userManager)
        {
            _dashboardService = dashboardService 
                ?? throw new ArgumentNullException(nameof(dashboardService));
            _logger = logger 
                ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                Guid? userId = GetUserId();
                ApplicationUser? user 
                    = await GetUserAsync(userId!.Value);

                if (!userId.HasValue)
                {
                    _logger.LogWarning("Failed to get user ID from claims.");
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                DashboardViewModel dashboardViewModel 
                    = await _dashboardService
                    .GetDashboardDataAsync(userId.Value, user);
                    
                // Create a view model that will work with our views
                // The dashboard uses Posts.PostCardViewModel but our partial view expects Community.PostCardViewModel
                if (dashboardViewModel.RecentPosts != null && dashboardViewModel.RecentPosts.Any())
                {
                    // Convert Posts.PostCardViewModel to Community.PostCardViewModel
                    var convertedPosts = dashboardViewModel.RecentPosts.Select(post => new ActioNator.ViewModels.Community.PostCardViewModel
                    {
                        Id = post.Id,
                        AuthorId = post.AuthorId,
                        AuthorName = post.AuthorName,
                        Content = post.Content,
                        CreatedAt = post.CreatedAt,
                        IsLiked = false, // Default value since Posts.PostCardViewModel doesn't have this property
                        LikesCount = post.LikesCount,
                        CommentsCount = post.CommentsCount,
                        SharesCount = post.SharesCount,
                        IsDeleted = post.IsDeleted,
                        ProfilePictureUrl = post.ProfilePictureUrl,
                        // Convert Images collection to the correct type
                        Images = post.Images?.Select(img => new ActioNator.ViewModels.Community.PostImageViewModel
                        {
                            Id = img.Id,
                            PostId = img.PostId ?? Guid.Empty, // Handle nullable PostId
                            ImageUrl = img.ImageUrl,
                            CreatedAt = DateTime.Now // PostImagesViewModel doesn't have CreatedAt, use current time
                        }).ToList() ?? new List<ActioNator.ViewModels.Community.PostImageViewModel>(),
                        // Convert Comments collection to the correct type
                        Comments = post.Comments?.Select(c => new ActioNator.ViewModels.Community.PostCommentViewModel
                        {
                            Id = c.Id,
                            Content = c.Content,
                            AuthorId = c.AuthorId,
                            AuthorName = c.AuthorName,
                            CreatedAt = c.CreatedAt,
                            ProfilePictureUrl = c.ProfilePictureUrl,
                            IsDeleted = c.IsDeleted
                        }).ToList() ?? new List<ActioNator.ViewModels.Community.PostCommentViewModel>()
                    }).ToList();
                    
                    // Store the converted posts in ViewBag for the view to use
                    ViewBag.ConvertedRecentPosts = convertedPosts;
                }

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                var currentUserId = _userManager.GetUserId(User) ?? "Unknown";
                _logger.LogError(ex, "Error occurred while loading dashboard for user {UserId}", currentUserId);
                return View("Error");
            }
        }
    }
}
