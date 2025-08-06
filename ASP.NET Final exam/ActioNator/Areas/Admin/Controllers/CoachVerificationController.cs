using FinalExamUI.Services.Interfaces;
using FinalExamUI.ViewModels.CoachVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FinalExamUI.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CoachVerificationController : Controller
    {
        private readonly ICoachVerificationService _coachVerificationService;

        public CoachVerificationController(ICoachVerificationService coachVerificationService)
        {
            _coachVerificationService = coachVerificationService;
        }

        public async Task<IActionResult> Index()
        {
            var verificationRequests = await _coachVerificationService.GetAllVerificationRequestsAsync();
            return View(verificationRequests);
        }

        [HttpGet]
        public async Task<IActionResult> UserVerificationPartial(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("User ID is required");
            }

            var documents = await _coachVerificationService.GetDocumentsForUserAsync(userId);
            
            var model = new CoachVerificationUserViewModel
            {
                UserId = userId,
                Documents = documents
            };

            return PartialView("_UserVerificationPartial", model);
        }

        [HttpGet]
        public IActionResult ViewDocument(string path)
        {
            // This is a placeholder for document viewing functionality
            // In a real implementation, you would validate the path and return the document
            if (string.IsNullOrEmpty(path) || path.Contains(".."))
            {
                return BadRequest("Invalid path");
            }

            return File(path, "application/octet-stream");
        }

        // Placeholder for future approval/rejection functionality
        [HttpPost]
        public IActionResult ApproveVerification(string userId)
        {
            // Placeholder for approval logic
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RejectVerification(string userId)
        {
            // Placeholder for rejection logic
            return RedirectToAction("Index");
        }
    }
}
