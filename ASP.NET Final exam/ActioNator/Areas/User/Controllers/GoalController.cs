using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Services.Interfaces.GoalService;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.ViewModels.Goal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    [Authorize]
    public class GoalController : BaseController
    {
        private readonly IGoalService _goalService;
        private readonly IInputSanitizationService _sanitizationService;
        private readonly ILogger<GoalController> _logger;

        public GoalController(
            IGoalService goalService,
            IInputSanitizationService sanitizationService,
            ILogger<GoalController> logger,
            UserManager<ApplicationUser> userManager) 
            : base(userManager)
        {
            _goalService = goalService 
                ?? throw new ArgumentNullException(nameof(goalService));
            _sanitizationService = sanitizationService
                ?? throw new ArgumentNullException(nameof(sanitizationService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                Guid? userId = GetUserId();
                IEnumerable<GoalViewModel> goals 
                    = await _goalService
                    .GetUserGoalsAsync(userId);

                return View(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals");
                return View(new List<GoalViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGoals(string filter = "all")
        {
            try
            {
                Guid? userId = GetUserId();
                IEnumerable<GoalViewModel>? goals 
                    = await _goalService
                    .GetUserGoalsAsync(userId, filter);

                return Json(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals with filter: {Filter}", filter);
                return Json(new List<GoalViewModel>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGoalPartial(string filter = "all")
        {
            try
            {
                Guid? userId = GetUserId();
                IEnumerable<GoalViewModel>? goals 
                    = await _goalService
                    .GetUserGoalsAsync(userId, filter);

                return PartialView("_GoalsPartial", goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals partial with filter: {Filter}", filter);
                return PartialView("_GoalsPartial", new List<GoalViewModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Create([FromBody] GoalViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Sanitize input
                model.Title 
                    = _sanitizationService
                    .SanitizeHtml(model.Title);

                model.Description 
                    = _sanitizationService
                    .SanitizeHtml(model.Description);

                Guid? userId = GetUserId();
                model 
                    = await _goalService
                    .CreateGoalAsync(model, userId);

                return Json(new { success = true, goal = model });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal");
                return Json(new { success = false, message = "Failed to create goal. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Update([FromBody] GoalViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state: {@ModelStateErrors}", 
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    
                    return BadRequest(new { 
                        success = false, 
                        message = "Invalid data provided", 
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList() 
                    });
                }

                // Sanitize inputs
                model.Title = _sanitizationService.SanitizeHtml(model.Title);
                model.Description = _sanitizationService.SanitizeHtml(model.Description);

                // Get current user
                var userId = GetUserId();
                if (userId == null)
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                // Update the goal
                var result = await _goalService.UpdateGoalAsync(model);
                
                return Json(new { success = true, goal = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal");
                return StatusCode(500, new { success = false, message = "An error occurred while updating the goal" });
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Delete([FromBody] DeleteGoalRequest request)
        {
            try
            {
                if (request == null || request.Id == Guid.Empty)
                {
                    return BadRequest("Goal ID is required");
                }

                Guid? userId = GetUserId();
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized attempt to delete goal: User not authenticated");
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                // Verify ownership
                if (!await _goalService.VerifyGoalOwnershipAsync(request.Id, userId))
                {
                    _logger.LogWarning("Unauthorized attempt to delete goal: User {UserId} attempted to delete goal {GoalId} they don't own", userId, request.Id);
                    // Return a JSON response instead of Forbid() to avoid the redirect
                    return StatusCode(403, new { success = false, message = "You do not have permission to delete this goal" });
                }

                await _goalService
                    .DeleteGoalAsync(request.Id);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal with ID: {GoalId}", request.Id);
                return StatusCode(500, new { success = false, message = "Failed to delete goal. Please try again." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> ToggleComplete(Guid goalId)
        {
            try
            {
                if (goalId == Guid.Empty)
                {
                    return BadRequest("Goal ID is required");
                }

                Guid? userId = GetUserId();

                // Verify ownership
                if (!await _goalService.VerifyGoalOwnershipAsync(goalId, userId))
                {
                    return Forbid();
                }

                GoalViewModel goal 
                    = await _goalService
                    .ToggleGoalCompletionAsync(goalId);

                return Json(new { success = true, goal });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling completion for goal with ID: {GoalId}", goalId);
                return Json(new { success = false, message = "Failed to update goal status. Please try again." });
            }
        }
    }
}
