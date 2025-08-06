using ActioNator.Services.Interfaces.ReportVerificationService;
using ActioNator.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ActioNator.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportReviewController : Controller
    {
        private readonly IReportReviewService _reportService;

        public ReportReviewController(IReportReviewService reportService)
        {
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            var reportedPosts = await _reportService.GetReportedPostsAsync();
            var reportedComments = await _reportService.GetReportedCommentsAsync();
            var model = new ReportReviewPageViewModel
            {
                ReportedPosts = reportedPosts,
                ReportedComments = reportedComments
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            var result = await _reportService.DeletePostAsync(id);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            var result = await _reportService.DeleteCommentAsync(id);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissPostReport(Guid id)
        {
            var result = await _reportService.DismissPostReportAsync(id);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissCommentReport(Guid id)
        {
            var result = await _reportService.DismissCommentReportAsync(id);
            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> ViewPost(Guid id)
        {
            // This would be implemented to fetch the full post content
            // For now, we'll return a placeholder
            return Json(new { id = id, content = "Full post content would be displayed here." });
        }

        [HttpGet]
        public async Task<IActionResult> ViewComment(Guid id)
        {
            // This would be implemented to fetch the full comment content
            // For now, we'll return a placeholder
            return Json(new { id = id, content = "Full comment content would be displayed here." });
        }
    }
}
