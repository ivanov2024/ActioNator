using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Extensions;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Models;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.WorkoutService;
using ActioNator.ViewModels.Workout;
using ActioNator.ViewModels.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ActioNator.Areas.User.Controllers
{

    [Authorize]
    [Area("User")]
    public class WorkoutController : BaseController
    {
        private readonly IWorkoutService _workoutService;
        private readonly IInputSanitizationService _sanitizationService;
        private readonly ILogger<WorkoutController> _logger;

        public WorkoutController(
            UserManager<Data.Models.ApplicationUser> userManager,
            IWorkoutService workoutService,
            IInputSanitizationService sanitizationService,
            ILogger<WorkoutController> logger)
            : base(userManager)
        {
            _workoutService = workoutService
                ?? throw new ArgumentNullException(nameof(workoutService));
            _sanitizationService = sanitizationService
                ?? throw new ArgumentNullException(nameof(sanitizationService));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkoutJson(Guid id)
        {
            Guid? userId = GetUserId();
            if (userId == null || userId == Guid.Empty)
            {
                return Json(new { success = false, toastType = "error", toastMessage = "User not authenticated." });
            }

            try
            {
                var workout = await _workoutService.GetWorkoutByIdAsync(id, userId);
                if (workout == null)
                {
                    return Json(new { success = false, toastType = "error", toastMessage = "Workout not found." });
                }

                // Load templates to enrich exercises with resolved imageUrl
                IEnumerable<ExerciseTemplateViewModel> templates = await _workoutService.GetExerciseTemplatesAsync();
                var templateMap = templates.ToDictionary(t => t.Id, t => t.ImageUrl);

                var exercises = (workout.Exercises ?? Enumerable.Empty<ExerciseViewModel>())
                    .Select(e => new
                    {
                        id = e.Id,
                        workoutId = e.WorkoutId,
                        name = e.Name,
                        sets = e.Sets,
                        reps = e.Reps,
                        weight = e.Weight,
                        notes = e.Notes,
                        duration = e.Duration,
                        exerciseTemplateId = e.ExerciseTemplateId,
                        targetedMuscle = e.TargetedMuscle,
                        imageUrl = templateMap.TryGetValue(e.ExerciseTemplateId, out var url) && !string.IsNullOrWhiteSpace(url)
                            ? Url.Content(url!)
                            : null
                    })
                    .ToArray();

                var workoutDto = new
                {
                    id = workout.Id,
                    title = workout.Title,
                    // Return duration in minutes for easier client handling
                    duration = (int)workout.Duration.TotalMinutes,
                    notes = workout.Notes,
                    date = workout.Date,
                    completedAt = workout.CompletedAt,
                    isCompleted = workout.CompletedAt.HasValue,
                    exercises
                };

                return Json(new { success = true, workout = workoutDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workout {WorkoutId} for user {UserId}: {ErrorMessage}", id, userId, ex.Message);
                return Json(new { success = false, toastType = "error", toastMessage = "Error loading workout." });
            }
        }

        private async Task<string?> ResolveExerciseImageUrl(Guid templateId)
        {
            try
            {
                if (templateId == Guid.Empty) return null;
                IEnumerable<ExerciseTemplateViewModel> templates = await _workoutService.GetExerciseTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Id == templateId);
                if (template == null || string.IsNullOrWhiteSpace(template.ImageUrl)) return null;
                return Url.Content(template.ImageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving image URL for template {TemplateId}: {ErrorMessage}", templateId, ex.Message);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Guid? userId = GetUserId();

            if (userId == null || userId == Guid.Empty)
            {
                return Unauthorized();
            }

            try
            {
                IEnumerable<WorkoutCardViewModel>? workouts
                    = await
                    _workoutService
                    .GetAllWorkoutsAsync(userId.Value);

                return View(workouts);
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Failed to retrieve workouts for user {UserId}", userId);
                return View("Error", "Unable to load workouts at this time.");
            }
        }

        [HttpGet]
        public IActionResult New()
        {
            return View("NewEditWorkout", new WorkoutCardViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkouts()
        {
            Guid? userId = GetUserId();

            bool isAjax
                = Request.Headers.XRequestedWith == "XMLHttpRequest";

            if (userId == null || userId == Guid.Empty)
            {
                _logger
                    .LogCritical("User not authenticated when attempting to get workouts");

                if (isAjax)
                {
                    ViewData["Success"] = false;
                    ViewData["ToastType"] = "error";
                    ViewData["ToastMessage"] = "Authentication error. Please log in again.";

                    return PartialView("_WorkoutResponsePartial", null);
                }

                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            try
            {
                IEnumerable<WorkoutCardViewModel>? workouts
                    = await
                    _workoutService
                    .GetAllWorkoutsAsync(userId);

                _logger
                    .LogInformation("Retrieved {Count} workouts for user {UserId}", workouts?.Count() ?? 0, userId);

                if (isAjax)
                {
                    return PartialView("_WorkoutsPartial", workouts);
                }

                return View("Index", workouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workouts for user {UserId}: {ErrorMessage}", userId, ex.Message);

                if (isAjax)
                {
                    ViewData["Success"] = false;
                    ViewData["ToastType"] = "error";
                    ViewData["ToastMessage"] = "Error retrieving workouts. Please try again.";
                    return PartialView("_WorkoutResponsePartial", null);
                }

                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || id == Guid.Empty)
            {
                return BadRequest();
            }

            Guid? userId = GetUserId();

            WorkoutCardViewModel? workout
                = await
                _workoutService
                .GetWorkoutByIdAsync(id, userId);

            if (workout == null)
            {
                return NotFound();
            }

            // Get exercise templates for the dropdown
            IEnumerable<ExerciseTemplateViewModel> exerciseTemplates
                = await
                _workoutService
                .GetExerciseTemplatesAsync();

            ViewBag
                .ExerciseOptions = exerciseTemplates;

            return View("NewEditWorkout", workout);
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        [Consumes("application/json")]
        public async Task<IActionResult> Edit([FromBody] WorkoutCardViewModel model)
        {
            Guid? userId = GetUserId();
            bool isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";

            if (userId == null || userId == Guid.Empty)
            {
                _logger.LogWarning("Unauthorized workout edit attempt.");
                if (isAjax)
                {
                    return Json(new { success = false, message = "Unauthorized access." });
                }
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for workout edit for user {UserId}", userId);

                if (isAjax)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = "Please fix the validation errors.",
                        validationErrors = errors
                    });
                }

                IEnumerable<ExerciseTemplateViewModel> exerciseTemplates = await _workoutService.GetExerciseTemplatesAsync();
                ViewBag.ExerciseOptions = exerciseTemplates;
                return View("NewEditWorkout", model);
            }

            try
            {
                await _workoutService.UpdateWorkoutAsync(model, userId);
                _logger.LogInformation("Workout {WorkoutId} updated successfully for user {UserId}", model.Id, userId);

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        toastType = "success",
                        toastMessage = "Workout updated successfully!"
                    });
                }

                TempData["SuccessMessage"] = "Workout updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout {WorkoutId} for user {UserId}: {ErrorMessage}",
                    model.Id, userId, ex.Message);

                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = $"Error updating workout: {ex.Message}"
                    });
                }

                ModelState.AddModelError("", $"Error updating workout: {ex.Message}");
                IEnumerable<ExerciseTemplateViewModel> exerciseTemplates = await _workoutService.GetExerciseTemplatesAsync();
                ViewBag.ExerciseOptions = exerciseTemplates;
                return View("NewEditWorkout", model);
            }
        }

        // Handles standard form submissions (application/x-www-form-urlencoded or multipart/form-data)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
        public async Task<IActionResult> EditForm(WorkoutCardViewModel model)
        {
            Guid? userId = GetUserId();
            bool isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";

            if (userId == null || userId == Guid.Empty)
            {
                _logger.LogWarning("Unauthorized workout edit attempt (form submission).");
                if (isAjax)
                {
                    return Json(new { success = false, message = "Unauthorized access." });
                }
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for workout edit (form) for user {UserId}", userId);

                if (isAjax)
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = "Please fix the validation errors.",
                        validationErrors = errors
                    });
                }

                IEnumerable<ExerciseTemplateViewModel> exerciseTemplates = await _workoutService.GetExerciseTemplatesAsync();
                ViewBag.ExerciseOptions = exerciseTemplates;
                return View("NewEditWorkout", model);
            }

            try
            {
                await _workoutService.UpdateWorkoutAsync(model, userId);
                _logger.LogInformation("Workout {WorkoutId} updated successfully (form) for user {UserId}", model.Id, userId);

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        toastType = "success",
                        toastMessage = "Workout updated successfully!"
                    });
                }

                TempData["SuccessMessage"] = "Workout updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout (form) {WorkoutId} for user {UserId}: {ErrorMessage}",
                    model.Id, userId, ex.Message);

                if (isAjax)
                {
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = $"Error updating workout: {ex.Message}"
                    });
                }

                ModelState.AddModelError("", $"Error updating workout: {ex.Message}");
                IEnumerable<ExerciseTemplateViewModel> exerciseTemplates = await _workoutService.GetExerciseTemplatesAsync();
                ViewBag.ExerciseOptions = exerciseTemplates;
                return View("NewEditWorkout", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> DeleteWorkout(Guid id)
        {
            Guid? userId = GetUserId();
            bool isAjax = Request.Headers.XRequestedWith == "XMLHttpRequest";

            if (userId == null || userId == Guid.Empty)
            {
                _logger
                    .LogWarning("Unauthorized attempt to delete workout {WorkoutId} due to missing user ID", id);

                if (isAjax)
                {
                    return Json(new { success = false, toastType = "error", toastMessage = "Unauthorized access." });
                }

                return Unauthorized();
            }

            try
            {
                _logger.LogInformation("Attempting to delete workout {WorkoutId} for user {UserId}", id, userId);

                bool isOwner
                    = await
                    _workoutService
                    .VerifyWorkoutOwnershipAsync(id, userId);

                if (!isOwner)
                {
                    _logger.LogWarning("Unauthorized workout deletion attempt for user {UserId}", userId);
                    if (isAjax)
                    {
                        return Json(new { success = false, toastType = "error", toastMessage = "Unauthorized operation." });
                    }
                    return Unauthorized();
                }

                bool result = await _workoutService.DeleteWorkoutAsync(id, userId);

                if (isAjax)
                {
                    if (result)
                    {
                        _logger.LogInformation("Workout {WorkoutId} deleted successfully for user {UserId}", id, userId);
                        return Json(new { success = true, toastType = "success", toastMessage = "Workout deleted successfully!" });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete workout {WorkoutId} for user {UserId}", id, userId);
                        return Json(new { success = false, toastType = "error", toastMessage = "Error deleting workout. Please try again." });
                    }
                }

                TempData["ToastType"] = result ? "success" : "error";
                TempData["ToastMessage"] = result ? "Workout deleted successfully!" : "Error deleting workout. Please try again.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when deleting workout {WorkoutId} for user {UserId}: {ErrorMessage}",
                    id, userId, ex.Message);

                return HandleExceptionResponse(isAjax, "Cannot delete this workout. It may be in use or already deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workout {WorkoutId} for user {UserId}: {ErrorMessage}",
                    id, userId, ex.Message);

                return HandleExceptionResponse(isAjax, "An unexpected error occurred while deleting the workout. Please try again.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> AddExercise([FromBody] ExerciseViewModel model)
        {
            Guid? userId = GetUserId();

            if (userId == null || userId == Guid.Empty)
            {
                _logger
                    .LogWarning("Unauthorized attempt to add exercise to workout {WorkoutId}", model.WorkoutId);

                return Json(new { success = false, toastType = "error", toastMessage = "Unauthorized access." });
            }

            // Patch: If Name is missing but ExerciseTemplateId is present, set Name from template
            if (string.IsNullOrWhiteSpace(model.Name)
                    && model.ExerciseTemplateId != Guid.Empty)
            {
                try
                {
                    IEnumerable<ExerciseTemplateViewModel> templates
                        = await
                        _workoutService
                        .GetExerciseTemplatesAsync();

                    ExerciseTemplateViewModel? template
                        = templates
                        .FirstOrDefault(t => t.Id == model.ExerciseTemplateId);

                    if (template != null)
                    {
                        model.Name = template.Name;
                        model.TargetedMuscle = template.TargetedMuscle;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching exercise templates for user {UserId}: {ErrorMessage}",
                        userId, ex.Message);
                }
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogWarning("Validation failed for adding exercise to workout {WorkoutId} for user {UserId}",
                    model.WorkoutId, userId);

                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = "Please provide all required exercise information.",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray())
                });
            }

            try
            {
                ExerciseViewModel result
                    = await
                    _workoutService
                    .AddExerciseAsync(model, userId);

                _logger
                    .LogInformation("Exercise added successfully to workout {WorkoutId} for user {UserId}",
                    model.WorkoutId, userId);

                int duration = 0;
                try
                {
                    WorkoutCardViewModel? workout
                        = await
                        _workoutService
                        .GetWorkoutByIdAsync(model.WorkoutId, userId);
                }
                catch (Exception ex)
                {
                    _logger
                        .LogError(ex, "Error getting workout duration for workout {WorkoutId}: {ErrorMessage}",
                        model.WorkoutId, ex.Message);
                }

                // Resolve exercise image URL from template (if available)
                string? imageUrlResolved = await ResolveExerciseImageUrl(result.ExerciseTemplateId);

                // Shape the exercise for JSON with camelCase + imageUrl
                var exerciseDto = new
                {
                    id = result.Id,
                    workoutId = result.WorkoutId,
                    name = result.Name,
                    sets = result.Sets,
                    reps = result.Reps,
                    weight = result.Weight,
                    notes = result.Notes,
                    duration = result.Duration,
                    exerciseTemplateId = result.ExerciseTemplateId,
                    targetedMuscle = result.TargetedMuscle,
                    imageUrl = imageUrlResolved
                };

                return Json(new
                {
                    success = true,
                    toastType = "success",
                    toastMessage = "Exercise added successfully!",
                    exercise = exerciseDto,
                    duration
                });
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error adding exercise to workout {WorkoutId} for user {UserId}: {ErrorMessage}",
                    model.WorkoutId, userId, ex.Message);

                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = "An unexpected error occurred while adding the exercise. Please try again."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> UpdateExercise([FromBody] ExerciseViewModel model)
        {
            Guid? userId = GetUserId();

            // Patch: If Name is missing but ExerciseTemplateId is present, set Name from template
            if (string.IsNullOrWhiteSpace(model.Name) && model.ExerciseTemplateId != Guid.Empty)
            {
                try
                {
                    IEnumerable<ExerciseTemplateViewModel> templates
                        = await
                        _workoutService
                        .GetExerciseTemplatesAsync();

                    ExerciseTemplateViewModel? template
                        = templates
                        .FirstOrDefault(t => t.Id == model.ExerciseTemplateId);
                    if (template != null)
                    {
                        model.Name = template.Name;
                        model.TargetedMuscle = template.TargetedMuscle;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching exercise templates for user {UserId}: {ErrorMessage}",
                        userId, ex.Message);
                }
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogWarning("Validation failed for updating exercise {ExerciseId} for user {UserId}",
                    model.Id, userId);

                // Return JSON with validation errors (consistent with AddExercise)
                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = "Please provide all required exercise information.",
                    errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    )
                });
            }

            try
            {
                ExerciseViewModel result
                    = await
                    _workoutService
                    .UpdateExerciseAsync(model, userId);

                _logger
                    .LogInformation("Exercise {ExerciseId} updated successfully for workout {WorkoutId} for user {UserId}",
                    model.Id, model.WorkoutId, userId);

                // Get updated workout duration with defensive coding
                WorkoutCardViewModel? workout
                    = await
                    _workoutService
                    .GetWorkoutByIdAsync(model.WorkoutId, userId);

                // Resolve exercise image URL from template (if available)
                string? imageUrlResolved = await ResolveExerciseImageUrl(result.ExerciseTemplateId);

                // Return JSON response for AJAX (consistent with AddExercise) with enriched exercise
                var exerciseDto = new
                {
                    id = result.Id,
                    workoutId = result.WorkoutId,
                    name = result.Name,
                    sets = result.Sets,
                    reps = result.Reps,
                    weight = result.Weight,
                    notes = result.Notes,
                    duration = result.Duration,
                    exerciseTemplateId = result.ExerciseTemplateId,
                    targetedMuscle = result.TargetedMuscle,
                    imageUrl = imageUrlResolved
                };

                return Json(new
                {
                    success = true,
                    toastType = "success",
                    toastMessage = "Exercise updated successfully!",
                    exercise = exerciseDto,
                });
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error updating exercise {ExerciseId} for workout {WorkoutId} for user {UserId}: {ErrorMessage}",
                    model.Id, model.WorkoutId, userId, ex.Message);

                // Return error response in JSON format (consistent with AddExercise)
                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = $"Error updating exercise: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> DeleteExercise([FromBody] ExerciseDeleteViewModel model)
        {
            Guid? userId = GetUserId();

            if (userId == null || userId == Guid.Empty)
            {
                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = "User not authenticated."
                });
            }

            try
            {
                // Verify ownership by exercise, not workout. The incoming model carries an exercise Id.
                bool isOwner =
                    await
                    _workoutService
                    .VerifyExerciseOwnershipAsync(model.Id, userId);

                if (!isOwner) return Unauthorized();

                bool result
                    = await
                    _workoutService
                    .DeleteExerciseAsync(model.Id, userId);

                if (result)
                {
                    _logger.LogInformation("Exercise {ExerciseId} deleted successfully for user {UserId}", model.Id, userId);

                    return Json(new
                    {
                        success = true,
                        toastType = "success",
                        toastMessage = "Exercise deleted successfully!"
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to delete exercise {ExerciseId} for user {UserId}", model.Id, userId);

                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = "Error deleting exercise. Please try again."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exercise {ExerciseId} for user {UserId}: {ErrorMessage}",
                    model.Id, userId, ex.Message);

                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = "Error deleting exercise. Please try again."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> CreateWorkout([FromBody] WorkoutCardViewModel model)
        {
            Guid? userId = GetUserId();
            bool isAjaxRequest = Request.Headers.XRequestedWith == "XMLHttpRequest";

            if (userId == null || userId == Guid.Empty)
            {
                _logger.LogWarning("CreateWorkout called without authenticated user");
                if (isAjaxRequest)
                {
                    return Json(new { success = false, toastType = "error", toastMessage = "User not authenticated." });
                }
                return Unauthorized(new { success = false, message = "User not authenticated." });
            }

            if (model == null)
            {
                _logger.LogWarning("CreateWorkout received null model. ContentType={ContentType}", Request.ContentType);
                if (isAjaxRequest)
                {
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = "Invalid request body. Please try again.",
                    });
                }
                return BadRequest(new { success = false, message = "Invalid request body." });
            }

            _logger.LogInformation("Received workout creation request from user {UserId} with title '{Title}' and {ExerciseCount} exercises",
                userId, model.Title, model.Exercises?.Count() ?? 0);

            // Sanitize user-provided input to prevent XSS/injection before validation and persistence
            SanitizeWorkoutModel(model);
            _logger.LogDebug("Workout model sanitized for user {UserId}", userId);

            // Re-validate model after sanitization so ModelState reflects the sanitized values
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed during workout creation for user {UserId}", userId);

                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

                if (isAjaxRequest)
                {
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = "Please fix the validation errors.",
                        validationErrors = errors
                    });
                }

                return View("NewEditWorkout", model);
            }

            try
            {
                WorkoutCardViewModel workout
                    = await
                    _workoutService
                    .CreateWorkoutAsync(model, userId);

                _logger.LogInformation("Workout {WorkoutId} created successfully for user {UserId}", workout.Id, userId);

                if (isAjaxRequest)
                {
                    IEnumerable<WorkoutCardViewModel>? workouts
                        = await
                        _workoutService
                        .GetAllWorkoutsAsync(userId);

                    string? workoutsHtml
                        = await
                        this.RenderPartialViewToStringAsync("_WorkoutsPartial", workouts);

                    return Json(new
                    {
                        success = true,
                        toastType = "success",
                        toastMessage = "Workout created successfully!",
                        workoutsHtml
                    });
                }

                TempData["SuccessMessage"] = "Workout created successfully!";
                TempData["ToastType"] = "success";
                TempData["ToastMessage"] = "Workout created successfully!";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workout for user {UserId}: {ErrorMessage}", userId, ex.Message);

                if (isAjaxRequest)
                {
                    return Json(new
                    {
                        success = false,
                        toastType = "error",
                        toastMessage = $"Error creating workout: {ex.Message}"
                    });
                }

                ModelState.AddModelError("", "An error occurred while creating the workout. Please try again.");
                return View("NewEditWorkout", model);
            }
        }

        /// <summary>
        /// Sanitizes all user-provided string inputs in a workout model to prevent XSS and injection attacks
        /// </summary>
        /// <param name="model">The workout model to sanitize</param>
        private void SanitizeWorkoutModel(WorkoutCardViewModel model)
        {
            if (model == null)
            {
                return;
            }

            try
            {
                // Sanitize workout properties
                model.Title =
                    _sanitizationService
                    .SanitizeString(model.Title);

                model.Notes =
                    _sanitizationService
                    .SanitizeString(model.Notes!);

                // Sanitize each exercise in the workout
                if (model.Exercises != null)
                {
                    foreach (var exercise in model.Exercises)
                    {
                        SanitizeExerciseModel(exercise);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing workout model");
                // Continue processing - we don't want to block the request due to sanitization errors
                // The model validation will catch any issues with the data
            }
        }

        /// <summary>
        /// Sanitizes all user-provided string inputs in an exercise model
        /// </summary>
        /// <param name="model">The exercise model to sanitize</param>
        private void SanitizeExerciseModel(ExerciseViewModel model)
        {
            if (model == null)
            {
                return;
            }

            try
            {
                model.Name =
                    _sanitizationService
                    .SanitizeString(model.Name);

                model.Notes =
                    _sanitizationService
                    .SanitizeString(model.Notes!);

                model.TargetedMuscle =
                    _sanitizationService
                    .SanitizeString(model.TargetedMuscle!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing exercise model");
            }
        }

        /// <summary>
        /// Helper method to handle exception responses consistently across controller actions.
        /// Returns JSON error for AJAX requests or redirects with error messages for standard requests.
        /// </summary>
        /// <param name="isAjaxRequest">Indicates whether the request is an AJAX request.</param>
        /// <param name="errorMessage">The error message to display to the user.</param>
        /// <returns>Appropriate IActionResult based on request type.</returns>
        private IActionResult HandleExceptionResponse(bool isAjaxRequest, string errorMessage)
        {
            if (isAjaxRequest)
            {
                // Return JSON error response for AJAX requests
                return Json(new
                {
                    success = false,
                    toastType = "error",
                    toastMessage = errorMessage
                });
            }

            // For regular form submissions, set TempData for toast notification and redirect
            TempData["ToastMessage"] = errorMessage;
            TempData["ToastType"] = "error";
            return RedirectToAction(nameof(Index));
        }
    }
}
