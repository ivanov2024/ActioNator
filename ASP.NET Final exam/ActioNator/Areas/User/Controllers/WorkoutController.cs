using ActioNator.Controllers;
using ActioNator.Extensions;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.WorkoutService;
using ActioNator.ViewModels.Workout;
using ActioNator.ViewModels.Workouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
                model.Title = _sanitizationService.SanitizeString(model.Title);
                model.Notes = _sanitizationService.SanitizeString(model.Notes);
                
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
                model.Name = _sanitizationService.SanitizeString(model.Name);
                model.Notes = _sanitizationService.SanitizeString(model.Notes);
                model.ImageUrl = _sanitizationService.SanitizeString(model.ImageUrl);
                model.TargetedMuscle = _sanitizationService.SanitizeString(model.TargetedMuscle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing exercise model");
                // Continue processing - we don't want to block the request due to sanitization errors
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            Guid? userId = GetUserId();
            
            var workouts 
                = await _workoutService.GetAllWorkoutsAsync(userId);
            return View(workouts);
        }
        
        [HttpGet]
        public IActionResult New()
        {
            return View("NewEditWorkout", new WorkoutCardViewModel());
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkoutCardViewModel model)
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";
            
            _logger.LogInformation("Attempting to create new workout for user {UserId}", userIdString);
            
            // Use the shared method to process the workout creation
            return await ProcessWorkoutSaveAsync(model, userId, userIdString, "Workout created successfully!", "Error creating workout. Please try again.");
        }
        
        /// <summary>
        /// Shared method to process workout creation or update
        /// </summary>
        /// <param name="model">The workout model to create or update</param>
        /// <param name="userId">The current user ID</param>
        /// <param name="userIdString">String representation of the user ID for logging</param>
        /// <param name="successMessage">Success message to display</param>
        /// <param name="errorMessage">Error message to display</param>
        /// <returns>Action result based on the operation outcome</returns>
        private async Task<IActionResult> ProcessWorkoutSaveAsync(
            WorkoutCardViewModel model, 
            Guid? userId, 
            string userIdString,
            string successMessage,
            string errorMessage)
        {
            try
            {
                // Sanitize user inputs
                SanitizeWorkoutModel(model);
                
                // Validate model state
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Validation failed for workout operation for user {UserId}", userIdString);
                    
                    // For AJAX requests, return validation errors
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        // Collect validation errors for the response
                        var errors = ModelState.ToDictionary(
                            // Keep the original property name casing for client-side matching
                            kvp => kvp.Key,
                            kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        );
                        
                        // Log the validation errors
                        _logger.LogWarning("Validation errors for workout: {@ValidationErrors}", errors);
                        
                        return Json(new { 
                            success = false, 
                            toastType = "error", 
                            toastMessage = "Please fix the validation errors.",
                            validationErrors = errors
                        });
                    }
                    
                    // For regular form submissions, return the view with errors
                    return View("NewEditWorkout", model);
                }
                
                // Verify ownership if this is an update operation
                if (model.Id != Guid.Empty)
                {
                    bool isOwner = await _workoutService
                        .VerifyWorkoutOwnershipAsync(model.Id, userId);
                    if (!isOwner)
                    {
                        _logger.LogWarning("Unauthorized workout update attempt for user {UserId}", userIdString);
                        return Unauthorized();
                    }
                }
                
                // Create or update the workout
                var workout = await _workoutService.CreateWorkoutAsync(model, userId);
                
                if (workout != null)
                {
                    _logger.LogInformation("Workout operation successful for user {UserId}", userIdString);
                    
                    // For AJAX requests, return success JSON with updated workouts list
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        // Get updated workouts list
                        var workouts = await _workoutService.GetAllWorkoutsAsync(userId);
                        
                        // Render the partial view to HTML string
                        var workoutsHtml = await this.RenderPartialViewToStringAsync("_WorkoutsPartial", workouts);
                        
                        // Return JSON response with success data and HTML
                        return Json(new { 
                            success = true, 
                            toastType = "success", 
                            toastMessage = successMessage,
                            workoutsHtml = workoutsHtml
                        });
                    }
                    
                    // For regular form submissions, redirect with success message
                    TempData["SuccessMessage"] = successMessage;
                    TempData["ToastType"] = "success";
                    TempData["ToastMessage"] = successMessage;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("Failed workout operation for user {UserId}", userIdString);
                    
                    // For AJAX requests, return error JSON
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { 
                            success = false, 
                            toastType = "error", 
                            toastMessage = errorMessage
                        });
                    }
                    
                    // For regular form submissions, return the view with error
                    ModelState.AddModelError("", errorMessage);
                    return View("NewEditWorkout", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during workout operation for user {UserId}: {ErrorMessage}", userIdString, ex.Message);
                
                // For AJAX requests, return error JSON
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = false, 
                        toastType = "error", 
                        toastMessage = "An unexpected error occurred. Please try again."
                    });
                }
                
                // For regular form submissions, return the view with error
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View("NewEditWorkout", model);
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetWorkouts()
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";
            
            if (userId == null)
            {
                _logger.LogWarning("User not authenticated when attempting to get workouts");
                
                // Check if this is an AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // For AJAX requests, return error data in a partial view
                    ViewData["Success"] = false;
                    ViewData["ToastType"] = "error";
                    ViewData["ToastMessage"] = "Authentication error. Please log in again.";
                    return PartialView("_WorkoutResponsePartial", null);
                }
                
                // For non-AJAX requests, redirect to login
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            
            try
            {
                var workouts = await _workoutService.GetAllWorkoutsAsync(userId);
                _logger.LogInformation("Retrieved {Count} workouts for user {UserId}", workouts?.Count() ?? 0, userIdString);
                
                // For AJAX requests, return partial view without layout
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_WorkoutsPartial", workouts);
                }
                
                // For non-AJAX requests, return full view with layout
                return View("Index", workouts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workouts for user {UserId}: {ErrorMessage}", userIdString, ex.Message);
                
                // For AJAX requests, return error data in a partial view
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    ViewData["Success"] = false;
                    ViewData["ToastType"] = "error";
                    ViewData["ToastMessage"] = "Error retrieving workouts. Please try again.";
                    return PartialView("_WorkoutResponsePartial", null);
                }
                
                // For non-AJAX requests, return error view
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            Guid? userId = GetUserId();
            WorkoutCardViewModel? workout 
                = await _workoutService
                .GetWorkoutByIdAsync(id, userId);
            
            if (workout == null)
            {
                return NotFound();
            }

            // Defensive coding: ensure we have valid durations
            if (workout != null && workout.Exercises != null && workout.Exercises.Any())
            {
                // Always recalculate duration to ensure accuracy
                TimeSpan totalDuration = TimeSpan.FromMinutes(workout.Exercises.Sum(e => e.Duration));
                workout.Duration = totalDuration;
            }
            
            // Get exercise templates for the dropdown
            IEnumerable<ExerciseTemplateViewModel> exerciseTemplates 
                = await _workoutService.GetExerciseTemplatesAsync();
            
            ViewBag
                .ExerciseOptions = exerciseTemplates;
            
            return View("NewEditWorkout", workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WorkoutCardViewModel model)
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Validation failed for workout edit for user {UserId}", userIdString);
                
                // Prepare validation errors for the partial view
                ViewData["Success"] = false;
                ViewData["ToastType"] = "error";
                ViewData["ToastMessage"] = "Please fix the validation errors.";
                ViewData["Errors"] = ModelState.ToDictionary(
                    // Keep the original property name casing for client-side matching
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                
                // For AJAX requests, return validation errors partial
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_WorkoutResponsePartial", null);
                }
                
                // For regular form submissions, return to the view with the model
                // Get exercise templates for the dropdown to ensure proper model is passed
                IEnumerable<ExerciseTemplateViewModel> exerciseTemplates 
                    = await _workoutService.GetExerciseTemplatesAsync();
                
                ViewBag.ExerciseOptions = exerciseTemplates;
                return View("NewEditWorkout", model);
            }

            try
            {
                await _workoutService.UpdateWorkoutAsync(model, userId);
                _logger.LogInformation("Workout {WorkoutId} updated successfully for user {UserId}", model.Id, userIdString);

                // Prepare success data for the partial view
                ViewData["Success"] = true;
                ViewData["ToastType"] = "success";
                ViewData["ToastMessage"] = "Workout updated successfully!";
                
                // For AJAX requests, return success partial view
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_WorkoutResponsePartial", null);
                }
                
                // For regular form submissions, redirect with success message
                TempData["SuccessMessage"] = "Workout updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout {WorkoutId} for user {UserId}: {ErrorMessage}", 
                    model.Id, userIdString, ex.Message);
                
                // Prepare error data for the partial view
                ViewData["Success"] = false;
                ViewData["ToastType"] = "error";
                ViewData["ToastMessage"] = $"Error updating workout: {ex.Message}";
                
                // For AJAX requests, return error partial view
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_WorkoutResponsePartial", null);
                }
                
                // For regular form submissions, return to view with error
                ModelState.AddModelError("", $"Error updating workout: {ex.Message}");
                // Get exercise templates for the dropdown to ensure proper model is passed
                IEnumerable<ExerciseTemplateViewModel> exerciseTemplates 
                    = await _workoutService.GetExerciseTemplatesAsync();
                
                ViewBag.ExerciseOptions = exerciseTemplates;
                return View("NewEditWorkout", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkout(Guid id)
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";
            
            try
            {
                _logger.LogInformation("Attempting to delete workout {WorkoutId} for user {UserId}", id, userIdString);
                
                // Verify ownership before deletion
                bool isOwner = await _workoutService
                    .VerifyWorkoutOwnershipAsync(id, userId);
                if (!isOwner)
                {
                    _logger.LogWarning("Unauthorized workout deletion attempt for user {UserId}", userIdString);
                    return Unauthorized();
                }
                
                bool result = await _workoutService.DeleteWorkoutAsync(id, userId);

                // For AJAX requests, return JSON response instead of partial view
                // This prevents potential issues with partial view rendering and Alpine.js reinitialization
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    if (result)
                    {
                        _logger.LogInformation("Workout {WorkoutId} deleted successfully for user {UserId}", id, userIdString);
                        
                        // Return success JSON response
                        return Json(new { 
                            success = true, 
                            toastType = "success", 
                            toastMessage = "Workout deleted successfully!" 
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Failed to delete workout {WorkoutId} for user {UserId}", id, userIdString);
                        
                        // Return error JSON response
                        return Json(new { 
                            success = false, 
                            toastType = "error", 
                            toastMessage = "Error deleting workout. Please try again." 
                        });
                    }
                }
                
                // For regular form submissions, redirect with appropriate message
                if (result)
                {
                    _logger.LogInformation("Workout {WorkoutId} deleted successfully for user {UserId}", id, userIdString);
                    TempData["SuccessMessage"] = "Workout deleted successfully!";
                    // Add toast notification for success
                    TempData["ToastType"] = "success";
                    TempData["ToastMessage"] = "Workout deleted successfully!";
                }
                else
                {
                    _logger.LogWarning("Failed to delete workout {WorkoutId} for user {UserId}", id, userIdString);
                    TempData["ErrorMessage"] = "Error deleting workout. Please try again.";
                    // Add toast notification for error
                    TempData["ToastType"] = "error";
                    TempData["ToastMessage"] = "Error deleting workout. Please try again.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                // Handle specific exception type for business logic errors
                _logger.LogWarning(ex, "Invalid operation when deleting workout {WorkoutId} for user {UserId}: {ErrorMessage}", 
                    id, userIdString, ex.Message);
                
                return HandleExceptionResponse(
                    isAjax: Request.Headers["X-Requested-With"] == "XMLHttpRequest",
                    errorMessage: "Cannot delete this workout. It may be in use or already deleted.");
            }
            catch (Exception ex)
            {
                // Handle general exceptions
                _logger.LogError(ex, "Error deleting workout {WorkoutId} for user {UserId}: {ErrorMessage}", 
                    id, userIdString, ex.Message);
                
                return HandleExceptionResponse(
                    isAjax: Request.Headers["X-Requested-With"] == "XMLHttpRequest",
                    errorMessage: "An unexpected error occurred while deleting the workout. Please try again.");
            }
        }
        
        /// <summary>
        /// Helper method to handle exception responses consistently across controller actions
        /// </summary>
        /// <param name="isAjax">Whether the request is an AJAX request</param>
        /// <param name="errorMessage">Error message to display</param>
        /// <returns>Appropriate action result based on request type</returns>
        private IActionResult HandleExceptionResponse(bool isAjax, string errorMessage)
        {
            if (isAjax)
            {
                // Return JSON error response for AJAX requests
                return Json(new { 
                    success = false, 
                    toastType = "error", 
                    toastMessage = errorMessage 
                });
            }
            
            // For regular form submissions
            TempData["ErrorMessage"] = errorMessage;
            TempData["ToastMessage"] = errorMessage;
            TempData["ToastType"] = "error";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExercise([FromBody] ExerciseViewModel model)
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";
            
            // Patch: If Name is missing but ExerciseTemplateId is present, set Name from template
            if (string.IsNullOrWhiteSpace(model.Name) && model.ExerciseTemplateId != Guid.Empty)
            {
                try {
                    // Get exercise templates (from service or cache)
                    var templates = await _workoutService.GetExerciseTemplatesAsync();
                    var template = templates.FirstOrDefault(t => t.Id == model.ExerciseTemplateId);
                    if (template != null)
                    {
                        model.Name = template.Name;
                        model.ImageUrl = template.ImageUrl;
                        model.TargetedMuscle = template.TargetedMuscle;
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error fetching exercise templates for user {UserId}: {ErrorMessage}", 
                        userIdString, ex.Message);
                }
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogWarning("Validation failed for adding exercise to workout {WorkoutId} for user {UserId}", 
                    model.WorkoutId, userIdString);
                
                // Return JSON with validation errors
                return Json(new { 
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
                ExerciseViewModel result = await _workoutService.AddExerciseAsync(model, userId);
                _logger.LogInformation("Exercise added successfully to workout {WorkoutId} for user {UserId}", 
                    model.WorkoutId, userIdString);
                
                // Get updated workout duration with defensive coding
                int duration = 0;
                try {
                    var workout = await _workoutService.GetWorkoutByIdAsync(model.WorkoutId, userId);
                    
                    if (workout != null)
                    {
                        // Use the duration from the service if available
                        duration = (int)workout.Duration.TotalMinutes;
                        
                        // If we need to recalculate it client-side as a fallback
                        if (workout.Exercises != null)
                        {
                            // Calculate duration client-side to avoid EF Core translation issues
                            duration = workout.Exercises.Sum(e => e.Duration);
                        }
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error getting workout duration for workout {WorkoutId}: {ErrorMessage}", 
                        model.WorkoutId, ex.Message);
                    // Continue with duration = 0, client can recalculate
                }
                
                // Return JSON response for AJAX
                return Json(new { 
                    success = true, 
                    toastType = "success", 
                    toastMessage = "Exercise added successfully!",
                    exercise = result,
                    duration = duration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding exercise to workout {WorkoutId} for user {UserId}: {ErrorMessage}", 
                    model.WorkoutId, userIdString, ex.Message);
                
                // Return error response in JSON format
                return Json(new { 
                    success = false, 
                    toastType = "error", 
                    toastMessage = $"Error adding exercise: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExercise([FromBody] ExerciseViewModel model)
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";

            // Patch: If Name is missing but ExerciseTemplateId is present, set Name from template
            if (string.IsNullOrWhiteSpace(model.Name) && model.ExerciseTemplateId != Guid.Empty)
            {
                try {
                    // Get exercise templates (from service or cache)
                    var templates = await _workoutService.GetExerciseTemplatesAsync();
                    var template = templates.FirstOrDefault(t => t.Id == model.ExerciseTemplateId);
                    if (template != null)
                    {
                        model.Name = template.Name;
                        model.ImageUrl = template.ImageUrl;
                        model.TargetedMuscle = template.TargetedMuscle;
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error fetching exercise templates for user {UserId}: {ErrorMessage}", 
                        userIdString, ex.Message);
                }
            }

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogWarning("Validation failed for updating exercise {ExerciseId} for user {UserId}", 
                    model.Id, userIdString);
            
            // Return JSON with validation errors (consistent with AddExercise)
            return Json(new { 
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
                ExerciseViewModel result = await _workoutService.UpdateExerciseAsync(model, userId);
                _logger.LogInformation("Exercise {ExerciseId} updated successfully for workout {WorkoutId} for user {UserId}", 
                    model.Id, model.WorkoutId, userIdString);
                
                // Get updated workout duration with defensive coding
                var workout = await _workoutService.GetWorkoutByIdAsync(model.WorkoutId, userId);
                
                // Defensive coding: ensure we have a valid duration
                int duration = 0;
                if (workout != null)
                {
                    // Use the duration from the service if available
                    duration = (int)workout.Duration.TotalMinutes;
                    
                    // Always recalculate client-side to ensure accuracy
                    if (workout.Exercises != null)
                    {
                        // Calculate duration client-side to avoid EF Core translation issues
                        duration = workout.Exercises.Sum(e => e.Duration);
                    }
                }
                
                // Return JSON response for AJAX (consistent with AddExercise)
        return Json(new { 
            success = true, 
            toastType = "success", 
            toastMessage = "Exercise updated successfully!",
            exercise = result,
            duration = duration
        });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exercise {ExerciseId} for workout {WorkoutId} for user {UserId}: {ErrorMessage}", 
                    model.Id, model.WorkoutId, userIdString, ex.Message);
                
                // Return error response in JSON format (consistent with AddExercise)
        return Json(new { 
            success = false, 
            toastType = "error", 
            toastMessage = $"Error updating exercise: {ex.Message}"
        });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExercise([FromBody] ExerciseDeleteViewModel model)
        {
            if (model == null || model.Id == Guid.Empty)
            {
                // Return JSON error response (consistent with other methods)
                return Json(new { 
                    success = false, 
                    toastType = "error", 
                    toastMessage = "Invalid exercise ID."
                });
            }
            
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";
            
            if (userId == null)
            {
                // Return JSON error response (consistent with other methods)
                return Json(new { 
                    success = false, 
                    toastType = "error", 
                    toastMessage = "User not authenticated."
                });
            }
            
            // Note: This is a simplified implementation that doesn't check if the exercise belongs
            // to the user. In a real application, we would need to verify ownership and potentially
            // to include the WorkoutId in the future. For now, we'll rely on client-side recalculation.
            
            try
            {
                bool result = await _workoutService.DeleteExerciseAsync(model.Id, userId);

                if (result)
                {
                    _logger.LogInformation("Exercise {ExerciseId} deleted successfully for user {UserId}", model.Id, userIdString);
                    
                    // Return JSON success response (consistent with other methods)
                    return Json(new { 
                        success = true, 
                        toastType = "success", 
                        toastMessage = "Exercise deleted successfully!"
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to delete exercise {ExerciseId} for user {UserId}", model.Id, userIdString);
                    
                    // Return JSON error response (consistent with other methods)
                    return Json(new { 
                        success = false, 
                        toastType = "error", 
                        toastMessage = "Error deleting exercise. Please try again."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exercise {ExerciseId} for user {UserId}: {ErrorMessage}", 
                    model.Id, userIdString, ex.Message);
                
                // Return JSON error response (consistent with other methods)
                return Json(new { 
                    success = false, 
                    toastType = "error", 
                    toastMessage = "Error deleting exercise. Please try again."
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkout(WorkoutCardViewModel model, string exercisesJson)
        {
            Guid? userId = GetUserId();
            string userIdString = userId?.ToString() ?? "Unknown";

            // Check if this is an AJAX request
            bool isAjaxRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            // Log the received exercisesJson
            _logger.LogInformation("Received exercisesJson: {ExercisesJson}", 
                exercisesJson?.Length > 100 ? exercisesJson.Substring(0, 100) + "..." : exercisesJson ?? "null");
                
            // Try to deserialize exercises from JSON if provided
            if (!string.IsNullOrEmpty(exercisesJson) && (model.Exercises == null || !model.Exercises.Any()))
            {
                try
                {
                    var exercises = System.Text.Json.JsonSerializer.Deserialize<List<ExerciseViewModel>>(exercisesJson);
                    if (exercises != null && exercises.Any())
                    {
                        _logger.LogInformation("Successfully deserialized {Count} exercises from JSON", exercises.Count);
                        model.Exercises = exercises;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deserializing exercises from JSON: {ErrorMessage}", ex.Message);
                }
            }
            
            // Log the full input model data for debugging
            _logger.LogInformation("Received workout data for user {UserId}: {@WorkoutData}", 
                userIdString, new { 
                    Title = model.Title, 
                    Notes = model.Notes,
                    ExercisesCount = model.Exercises?.Count() ?? 0,
                    HasExercises = model.Exercises != null && model.Exercises.Any(),
                    FirstExerciseName = model.Exercises?.FirstOrDefault()?.Name,
                    ModelState = ModelState.IsValid,
                    ModelStateKeys = string.Join(", ", ModelState.Keys)
                });
                
            // Log all form values for debugging
            foreach (var key in Request.Form.Keys)
            {
                var values = Request.Form[key];
                _logger.LogInformation("Form value {Key}: {Value}", key, string.Join(", ", values));
            }

            if (!ModelState.IsValid)
            {
                // Log all model state errors
                foreach (var modelStateKey in ModelState.Keys)
                {
                    var modelStateVal = ModelState[modelStateKey];
                    foreach (var error in modelStateVal.Errors)
                    {
                        _logger.LogWarning("Validation error for {Key}: {ErrorMessage}", 
                            modelStateKey, error.ErrorMessage);
                    }
                }
                
                _logger.LogWarning("Validation failed for workout creation for user {UserId}", userIdString);
                
                // Prepare validation errors for the response
                ViewData["Success"] = false;
                ViewData["ToastType"] = "error";
                ViewData["ToastMessage"] = "Please fix the validation errors.";
                var errors = ModelState.ToDictionary(
                    // Keep the original property name casing for client-side matching
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                ViewData["Errors"] = errors;
                
                // Log the validation errors dictionary
                _logger.LogWarning("Validation errors: {@ValidationErrors}", errors);
                
                // For AJAX requests, return partial view with validation errors
                if (isAjaxRequest)
                {
                    return Json(new { 
                        success = false, 
                        toastType = "error", 
                        toastMessage = "Please fix the validation errors.",
                        validationErrors = errors
                    });
                }
                
                // For non-AJAX requests, return to the form with model state errors
                return View("Index", await _workoutService.GetAllWorkoutsAsync(userId));
            }

            try
            {
                var workout = await _workoutService.CreateWorkoutAsync(model, userId);
                
                _logger.LogInformation("Workout {WorkoutId} created successfully for user {UserId}", workout.Id, userIdString);
                
                // Prepare success data for the response
                ViewData["Success"] = true;
                ViewData["ToastType"] = "success";
                ViewData["ToastMessage"] = "Workout created successfully!";
                
                // For AJAX requests, return JSON with success data and updated workout list
                if (isAjaxRequest)
                {
                    // Get the updated list of workouts to include in the response
                    var workouts = await _workoutService.GetAllWorkoutsAsync(userId);
                    
                    // Render the partial view to HTML string
                    var workoutsHtml = await this.RenderPartialViewToStringAsync("_WorkoutsPartial", workouts);
                    
                    // Return JSON response with success data and HTML for client-side update
                    return Json(new { 
                        success = true, 
                        toastType = "success", 
                        toastMessage = "Workout created successfully!",
                        workoutsHtml = workoutsHtml
                    });
                }
                
                // For non-AJAX requests, redirect to index with success message in TempData
                TempData["SuccessMessage"] = "Workout created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating workout for user {UserId}: {ErrorMessage}", userIdString, ex.Message);
                
                // Prepare error data for the response
                ViewData["Success"] = false;
                ViewData["ToastType"] = "error";
                ViewData["ToastMessage"] = "An error occurred while creating the workout. Please try again.";
                
                // For AJAX requests, return partial view with error data
                if (isAjaxRequest)
                {
                    return PartialView("_WorkoutResponsePartial", null);
                }
                
                // For non-AJAX requests, return to the form with error message
                ModelState.AddModelError("", "An error occurred while creating the workout. Please try again.");
                return View("Index", await _workoutService.GetAllWorkoutsAsync(userId));
            }
        }
    }
}
