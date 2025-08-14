using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.CoachVerification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ActioNator.Services.Interfaces.FileServices;
using System;
using System.IO;
using Microsoft.AspNetCore.Identity;
using ActioNator.Data.Models;
using ActioNator.GCommon;
using System.Linq;

namespace ActioNator.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CoachVerificationController : Controller
    {
        private readonly ICoachVerificationService _coachVerificationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoachVerificationController(
            ICoachVerificationService coachVerificationService,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUser> userManager)
        {
            _coachVerificationService = coachVerificationService;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
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

            // Find user and assign Coach role if needed
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Ensure the user has ONLY the Coach role after approval
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles
                .Where(r => !string.Equals(r, RoleConstants.Coach, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            IdentityResult roleResult = IdentityResult.Success;

            if (rolesToRemove.Length > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    TempData["Error"] = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Index");
                }
            }

            if (!await _userManager.IsInRoleAsync(user, RoleConstants.Coach))
            {
                roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.Coach);
            }

            bool flagUpdated = await _coachVerificationService.ApproveVerificationAsync(userId);

            if (roleResult.Succeeded && flagUpdated)
            {
                TempData["Success"] = "User has been approved as Coach.";
            }
            else
            {
                var errors = roleResult.Succeeded
                    ? string.Empty
                    : string.Join("; ", roleResult.Errors.Select(e => e.Description));
                if (!flagUpdated)
                {
                    errors = string.IsNullOrWhiteSpace(errors)
                        ? "Failed to update verification status."
                        : errors + "; Failed to update verification status.";
                }
                TempData["Error"] = string.IsNullOrWhiteSpace(errors) ? "Failed to approve verification." : errors;
            }

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
