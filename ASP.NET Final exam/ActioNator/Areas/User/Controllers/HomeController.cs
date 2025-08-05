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
