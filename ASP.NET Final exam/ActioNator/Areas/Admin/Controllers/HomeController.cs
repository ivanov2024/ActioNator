using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.ReportVerificationService;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActioNator.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class HomeController : BaseController
    {
        private readonly ICoachVerificationService _coachVerificationService;
        private readonly IReportReviewService _reportReviewService;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            ICoachVerificationService coachVerificationService,
            IReportReviewService reportReviewService) 
            : base(userManager)
        {
            _coachVerificationService = coachVerificationService;
            _reportReviewService = reportReviewService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var pendingCoaches = await _coachVerificationService.GetPendingVerificationsCountAsync();
            var pendingPosts = await _reportReviewService.GetPendingPostReportsCountAsync();
            var pendingComments = await _reportReviewService.GetPendingCommentReportsCountAsync();

            var model = new AdminDashboardCountsViewModel
            {
                PendingCoachVerifications = pendingCoaches,
                PendingPostReports = pendingPosts,
                PendingCommentReports = pendingComments,
                PendingUserReports = 0 // No user report entity; keep for future use
            };

            return View(model);
        }
    }
}
