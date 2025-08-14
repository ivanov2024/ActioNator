using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.CoachVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ActioNator.Services.Interfaces.FileServices;
using System;
using System.IO;

namespace ActioNator.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CoachVerificationController : Controller
    {
        private readonly ICoachVerificationService _coachVerificationService;
        private readonly IFileStorageService _fileStorageService;

        public CoachVerificationController(ICoachVerificationService coachVerificationService, IFileStorageService fileStorageService)
        {
            _coachVerificationService = coachVerificationService;
            _fileStorageService = fileStorageService;
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

            // Prevent direct navigation to the partial which would render without layout (can cause Quirks Mode)
            if (!Request.Headers.ContainsKey("X-Requested-With") ||
                !string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> ViewDocument(string userId, string path, bool download = false)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(path) || path.Contains(".."))
            {
                return BadRequest("Invalid request.");
            }

            try
            {
                var (fileStream, contentType) = await _fileStorageService.GetFileAsync(path, userId);
                string fileName = Path.GetFileName(path);

                if (download)
                {
                    return File(fileStream, contentType, fileName);
                }

                return File(fileStream, contentType);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVerification(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            bool success = await _coachVerificationService.ApproveVerificationAsync(userId);
            TempData[success ? "Success" : "Error"] = success ? "Verification approved." : "Failed to approve verification.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(string userId, string reason = "Rejected by admin")
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] = "User ID is required.";
                return RedirectToAction("Index");
            }

            bool success = await _coachVerificationService.RejectVerificationAsync(userId, reason);
            TempData[success ? "Success" : "Error"] = success ? "Verification rejected." : "Failed to reject verification.";
            return RedirectToAction("Index");
        }
    }
}
