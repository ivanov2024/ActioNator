using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ActioNator.Infrastructure.Attributes;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ActioNator.Areas.User.Controllers
{
    /// <summary>
    /// Controller for handling coach verification operations with enhanced security
    /// </summary>
    [Authorize]
    [Area("User")]
    [Route("[area]/[controller]/[action]")]
    public class CoachVerificationController : BaseController
    {
        private readonly ICoachDocumentUploadService _documentUploadService;
        private readonly ILogger<CoachVerificationController> _logger;
        
        /// <summary>
        /// Initializes a new instance of the CoachVerificationController class
        /// </summary>
        /// <param name="documentUploadService">Service for handling document uploads</param>
        /// <param name="logger">Logger instance</param>
        public CoachVerificationController(
            ICoachDocumentUploadService documentUploadService,
            ILogger<CoachVerificationController> logger,
            UserManager<ApplicationUser> userManager)
            : base(userManager)
        {
            _documentUploadService = documentUploadService ?? throw new ArgumentNullException(nameof(documentUploadService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Displays the coach verification page
        /// </summary>
        /// <returns>View for coach verification</returns>
        [HttpGet]
        public IActionResult VerifyCoach()
        {
            return View();
        }

        /// <summary>
        /// Handles the upload of coach verification documents with enhanced security
        /// </summary>
        /// <param name="files">Collection of files to upload</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>JSON result with upload status and details</returns>
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        [Consumes("multipart/form-data")]
        [Produces("application/json", "application/problem+json")]
        [RequestSizeLimit(104857600)] // 100MB in bytes - explicit limit at controller level
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)] // 100MB in bytes
        public async Task<IActionResult> UploadDocuments(
            [FromForm] List<IFormFile> files,
            CancellationToken cancellationToken)
        {
            try
            {
                Guid? userIdGuid = this.GetUserId();
                
                if (!userIdGuid.HasValue)
                {
                    _logger.LogWarning("User ID not found in claims for document upload");
                    return Unauthorized(new ProblemDetails
                    {
                        Title = "Authentication Error",
                        Detail = "User identification failed. Please log in again.",
                        Status = StatusCodes.Status401Unauthorized
                    });
                }
                
                _logger.LogInformation("Processing document upload request for user {UserId} with {Count} files", 
                    userIdGuid.Value, files?.Count ?? 0);
                
                // Validate files collection is not null
                if (files == null || files.Count == 0)
                {
                    _logger.LogWarning("No files provided for upload by user {UserId}", userIdGuid.Value);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "No Files Provided",
                        Detail = "No files were provided for upload.",
                        Status = StatusCodes.Status400BadRequest
                    });
                }
                
                // Adapt List<IFormFile> to IFormFileCollection expected by the service
                var formFiles = new FormFileCollection();
                foreach (var f in files)
                {
                    formFiles.Add(f);
                }

                // Process the upload using the service
                var result 
                    = await _documentUploadService
                    .ProcessUploadAsync(formFiles, userIdGuid.Value.ToString(), cancellationToken);
                
                // Return appropriate response based on the result
                if (result.Success)
                {
                    // On any new upload, ensure the user is marked as pending verification
                    var user = await _userManager.FindByIdAsync(userIdGuid.Value.ToString());
                    if (user != null)
                    {
                        user.IsVerifiedCoach = false;
                        // Store any non-null reference to uploaded docs so counters work
                        user.CoachDegreeFilePath = result.Files?.FirstOrDefault()?.FilePath
                            ?? $"App_Data/coach-verifications/{userIdGuid.Value}";

                        await _userManager.UpdateAsync(user);
                    }

                    return Ok(result);
                }
                else
                {
                    // Map error types to appropriate status codes
                    int statusCode = result.Error?.ErrorType switch
                    {
                        "FileSizeExceeded" => StatusCodes.Status413PayloadTooLarge,
                        "InvalidFileType" => StatusCodes.Status415UnsupportedMediaType,
                        "FileValidationFailed" => StatusCodes.Status400BadRequest,
                        "MixedFileTypes" => StatusCodes.Status400BadRequest,
                        "OperationCancelled" => StatusCodes.Status408RequestTimeout,
                        _ => StatusCodes.Status500InternalServerError
                    };
                    
                    // Create problem details for structured error response
                    var problemDetails = new ProblemDetails
                    {
                        Title = result.Error?.ErrorType ?? "Error",
                        Detail = result.Message,
                        Status = statusCode,
                        Instance = HttpContext.Request.Path
                    };
                    
                    // Add additional details if available
                    if (result.Error?.AdditionalInfo != null)
                    {
                        foreach (KeyValuePair<string,object> info in result.Error.AdditionalInfo)
                        {
                            problemDetails.Extensions[info.Key] = info.Value;
                        }
                    }
                    
                    return StatusCode(statusCode, problemDetails);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Document upload operation was cancelled");
                return StatusCode(StatusCodes.Status408RequestTimeout, new ProblemDetails
                {
                    Title = "Request Timeout",
                    Detail = "The operation was cancelled due to timeout.",
                    Status = StatusCodes.Status408RequestTimeout
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing document upload");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Server Error",
                    Detail = "An unexpected error occurred while processing your request.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}
