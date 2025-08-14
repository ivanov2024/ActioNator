using System;
using System.Threading.Tasks;
using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.UserProfileService;
using FinalExamUI.ViewModels.UserProfile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Interfaces;
using ActioNator.Services.Exceptions;
using Microsoft.AspNetCore.Http;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography;
using System.Text;
using ActioNator.Services.Interfaces.Security;
using Microsoft.Extensions.Logging;
using ActioNator.Infrastructure.Attributes;
using Microsoft.AspNetCore.Antiforgery;
using System.Text.Encodings.Web;
using ActioNator.GCommon;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.Community;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    public class UserProfileController : BaseController
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileValidatorFactory _fileValidatorFactory;
        private readonly IDropboxPictureService _dropboxPictureService;
        private readonly IDropboxOAuthService _dropboxOAuthService;
        private readonly IOptionsSnapshot<DropboxOptions> _dropboxOptions;
        private readonly ITokenProtector _tokenProtector;
        private readonly ILogger<UserProfileController> _logger;
        private readonly IAntiforgery _antiforgery;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDropboxTokenProvider _dropboxTokenProvider;
        private readonly IInputSanitizationService _inputSanitizationService;
        private readonly ICommunityService _communityService;

        public UserProfileController(
            IUserProfileService userProfileService,
            UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorageService,
            IFileValidatorFactory fileValidatorFactory,
            IDropboxPictureService dropboxPictureService,
            IDropboxOAuthService dropboxOAuthService,
            IOptionsSnapshot<DropboxOptions> dropboxOptions,
            ITokenProtector tokenProtector,
            ILogger<UserProfileController> logger,
            IAntiforgery antiforgery,
            IDropboxTokenProvider dropboxTokenProvider,
            IInputSanitizationService inputSanitizationService,
            ICommunityService communityService) : base(userManager)
        {
            _userProfileService = userProfileService;
            _userManager = userManager;
            _fileStorageService = fileStorageService;
            _fileValidatorFactory = fileValidatorFactory;
            _dropboxPictureService = dropboxPictureService;
            _dropboxOAuthService = dropboxOAuthService;
            _dropboxOptions = dropboxOptions;
            _tokenProtector = tokenProtector;
            _logger = logger;
            _antiforgery = antiforgery;
            _dropboxTokenProvider = dropboxTokenProvider;
            _inputSanitizationService = inputSanitizationService;
            _communityService = communityService;
        }

        // Detect Dropbox expired access token errors from wrapped exceptions
        private static bool ExceptionIndicatesExpiredToken(Exception ex)
        {
            try
            {
                // Scan entire exception text, including inner exceptions
                var text = ex.ToString();
                return text.IndexOf("expired_access_token", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        // Detect Dropbox missing scope errors from wrapped exceptions
        private static bool ExceptionIndicatesMissingScope(Exception ex)
        {
            try
            {
                var text = ex.ToString();
                if (string.IsNullOrEmpty(text)) return false;
                return text.IndexOf("required scope", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("missing_scope", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("does not have the required scope", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.IndexOf("not permitted to access this endpoint because it does not have the required scope", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Issues a fresh anti-forgery token and sets the matching cookie. Useful after popup OAuth flows that may update the cookie.
        /// </summary>
        [HttpGet]
        [IgnoreAntiforgeryToken]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult AntiforgeryRefresh()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Json(new { token = tokens.RequestToken });
        }

    // Class members continue here
        private readonly IUserProfileService _userProfileService;

        /// <summary>
        /// Displays the main profile page with the default tab
        /// </summary>
        /// <param name="userId">The ID of the user whose profile to display</param>
        /// <returns>The profile view</returns>
        public async Task<IActionResult> Index()
        {
            Guid? userId = GetUserId();
            if (userId == null)
                return NotFound();

            var profileViewModel = await _userProfileService.GetUserProfileAsync(userId.Value);
            
            if (profileViewModel == null)
            {
                return NotFound();
            }

            // Populate role flags for badge rendering
            var appUser = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (appUser != null)
            {
                profileViewModel.IsAdmin = await _userManager.IsInRoleAsync(appUser, RoleConstants.Admin);
            }

            return View(profileViewModel);
        }

        /// <summary>
        /// Initiates Dropbox OAuth using PKCE and redirects the user to Dropbox's consent page.
        /// </summary>
        [HttpGet]
        public IActionResult ConnectDropbox([FromQuery] string mode)
        {
            var appKey = _dropboxOptions.Value.AppKey;
            if (string.IsNullOrWhiteSpace(appKey))
            {
                return BadRequest("Dropbox AppKey is not configured.");
            }

            var redirectUri = _dropboxOptions.Value.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/signin-dropbox";
            var isPopup = string.Equals(mode, "popup", StringComparison.OrdinalIgnoreCase);
            if (isPopup)
            {
                // Append popup indicator for callback to return a lightweight HTML that posts a message and closes
                redirectUri = redirectUri.Contains("?") ? ($"{redirectUri}&popup=1") : ($"{redirectUri}?popup=1");
            }

            // Generate PKCE codes and random state
            var pkce = _dropboxOAuthService.GeneratePkceCodes();
            var stateBytes = RandomNumberGenerator.GetBytes(32);
            var state = WebEncoders.Base64UrlEncode(stateBytes);

            // Store verifier and state in session
            HttpContext.Session.SetString("Dropbox:CodeVerifier", pkce.CodeVerifier);
            HttpContext.Session.SetString("Dropbox:State", state);

            var authorizeUri = _dropboxOAuthService.BuildAuthorizeUri(appKey!, redirectUri, state, pkce.CodeChallenge);
            return Redirect(authorizeUri.ToString());
        }

        /// <summary>
        /// Returns the Friends tab partial view
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>The Friends partial view</returns>
        public async Task<IActionResult> GetFriendsPartial(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid guid))
            {
                return NotFound();
            }

            var viewModel = await _userProfileService.GetFriendsTabAsync(guid);
            
            if (viewModel == null)
            {
                return NotFound();
            }

            return PartialView("_FriendsPartial", viewModel);
        }

        /// <summary>
        /// Returns the About tab partial (single-block about text)
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>_AboutPartial with string model</returns>
        [HttpGet]
        public async Task<IActionResult> GetAboutPartial(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid guid))
            {
                return NotFound();
            }

            var profile = await _userProfileService.GetUserProfileAsync(guid);
            if (profile == null)
            {
                return NotFound();
            }

            return PartialView("_AboutPartial", profile.AboutText ?? string.Empty);
        }

        /// <summary>
        /// Returns the Posts tab partial with posts authored by the specified user.
        /// </summary>
        /// <param name="userId">Profile user's ID (author)</param>
        /// <param name="pageNumber">Page number (1-based)</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="status">Optional status filter (admins only)</param>
        [HttpGet]
        public async Task<IActionResult> GetUserPosts(string userId, int pageNumber = 1, int pageSize = 20, string status = null)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid authorId))
            {
                return NotFound();
            }

            var currentUserId = GetUserId();
            if (currentUserId == null)
            {
                return Unauthorized();
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId.Value.ToString());
            var isAdmin = currentUser != null && await _userManager.IsInRoleAsync(currentUser, RoleConstants.Admin);

            var posts = await _communityService.GetPostsByAuthorAsync(
                currentUserId.Value,
                authorId,
                status,
                pageNumber,
                pageSize,
                isAdmin);

            return PartialView("_PostsListPartial", posts);
        }

        /// <summary>
        /// Handles updating the user's profile information
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = GetUserId();
            var isAjax = IsAjaxRequest();
            if (userId == null)
            {
                if (isAjax) return Json(new { success = false, toastType = "error", toastMessage = "Unauthorized", message = "Unauthorized" });
                return Unauthorized();
            }

            try
            {
                var form = await Request.ReadFormAsync();
                
                // Handle profile picture upload if provided
                string profilePictureUrl = null;
                if (form.Files["profilePicture"] != null && form.Files["profilePicture"].Length > 0)
                {
                    var profilePic = form.Files["profilePicture"];
                    var validator = _fileValidatorFactory.GetValidatorForFile(profilePic);
                    var validationResult = await validator.ValidateAsync(profilePic);
                    if (!validationResult.IsValid)
                    {
                        if (isAjax)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                validationErrors = new { profilePicture = new[] { validationResult.ErrorMessage } },
                                toastType = "error",
                                toastMessage = validationResult.ErrorMessage
                            });
                        }
                        return BadRequest(validationResult.ErrorMessage);
                    }
                    // Defer actual storage decision (Dropbox vs Local) until after we know connection state
                }

                // Handle cover photo upload if provided
                string coverPhotoUrl = null;
                if (form.Files["coverPhoto"] != null && form.Files["coverPhoto"].Length > 0)
                {
                    var coverPhoto = form.Files["coverPhoto"];
                    var validator = _fileValidatorFactory.GetValidatorForFile(coverPhoto);
                    var validationResult = await validator.ValidateAsync(coverPhoto);
                    if (!validationResult.IsValid)
                    {
                        if (isAjax)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                validationErrors = new { coverPhoto = new[] { validationResult.ErrorMessage } },
                                toastType = "error",
                                toastMessage = validationResult.ErrorMessage
                            });
                        }
                        return BadRequest(validationResult.ErrorMessage);
                    }

                    // Save cover photo to App_Data
                    coverPhotoUrl = (await _fileStorageService.SaveFilesAsync(
                        new FormFileCollection { coverPhoto }, 
                        "App_Data/cover-photos", 
                        userId.Value.ToString())).FirstOrDefault();
                }

                // Update basic user info
                var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                if (user == null)
                    return NotFound("User not found");

                // Validate and sanitize FirstName/LastName using ValidationConstants
                var firstNameInput = form["firstName"].FirstOrDefault();
                var lastNameInput = form["lastName"].FirstOrDefault();

                var fieldErrors = new Dictionary<string, string[]>();

                // About text validation (client also validates; server is source of truth)
                var aboutInput = form["aboutText"].FirstOrDefault();
                string aboutSanitized = null;
                if (aboutInput != null)
                {
                    // Do not trim so user can intentionally keep leading spaces/newlines; enforce max length only
                    int aMax = ValidationConstants.UserProfile.AboutTextMaxLength;
                    if (aboutInput.Length > aMax)
                    {
                        fieldErrors["aboutText"] = new[] { $"About must be at most {aMax} characters." };
                    }
                    else
                    {
                        aboutSanitized = _inputSanitizationService.SanitizeString(aboutInput);
                    }
                }

                if (firstNameInput != null)
                {
                    var firstTrim = firstNameInput.Trim();
                    int fMin = ValidationConstants.ApplicationUser.FirstNameMinLength;
                    int fMax = ValidationConstants.ApplicationUser.FirstNameMaxLength;
                    if (firstTrim.Length < fMin || firstTrim.Length > fMax)
                    {
                        fieldErrors["firstName"] = new[] { $"First name must be between {fMin} and {fMax} characters." };
                    }
                    else
                    {
                        user.FirstName = _inputSanitizationService.SanitizeString(firstTrim);
                    }
                }

                if (lastNameInput != null)
                {
                    var lastTrim = lastNameInput.Trim();
                    int lMin = ValidationConstants.ApplicationUser.LastNameMinLength;
                    int lMax = ValidationConstants.ApplicationUser.LastNameMaxLength;
                    if (lastTrim.Length < lMin || lastTrim.Length > lMax)
                    {
                        fieldErrors["lastName"] = new[] { $"Last name must be between {lMin} and {lMax} characters." };
                    }
                    else
                    {
                        user.LastName = _inputSanitizationService.SanitizeString(lastTrim);
                    }
                }

                if (fieldErrors.Count > 0)
                {
                    if (isAjax)
                    {
                        return BadRequest(new { success = false, validationErrors = fieldErrors, toastType = "error", toastMessage = "Please fix the validation errors" });
                    }
                    return BadRequest("Validation failed");
                }
                
                // If a profile picture is provided, obtain an access token via centralized provider
                if (form.Files["profilePicture"] != null && form.Files["profilePicture"].Length > 0)
                {
                    var tokenResult = await _dropboxTokenProvider.GetAccessTokenAsync(userId.Value);
                    if (!tokenResult.Success)
                    {
                        if (tokenResult.RequiresUserConsent)
                        {
                            // AJAX: instruct client to open OAuth popup with authorizeUrl; non-AJAX: keep 428
                            var appKey = _dropboxOptions.Value.AppKey;
                            if (isAjax)
                            {
                                if (string.IsNullOrWhiteSpace(appKey))
                                {
                                    _logger.LogError("Dropbox AppKey not configured (AJAX) while updating profile for user {UserId} trace {TraceId}", userId, HttpContext.TraceIdentifier);
                                    return StatusCode(500, new { success = false, toastType = "error", toastMessage = "An unexpected error occurred", message = "An unexpected error occurred" });
                                }

                                // Build authorize URL and stash PKCE + state in session
                                var pkce = _dropboxOAuthService.GeneratePkceCodes();
                                var stateBytes = RandomNumberGenerator.GetBytes(32);
                                var state = WebEncoders.Base64UrlEncode(stateBytes);
                                HttpContext.Session.SetString("Dropbox:CodeVerifier", pkce.CodeVerifier);
                                HttpContext.Session.SetString("Dropbox:State", state);
                                var redirectUri = _dropboxOptions.Value.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/signin-dropbox";
                                // Force popup mode so callback returns lightweight HTML that postMessages
                                redirectUri = redirectUri.Contains("?") ? ($"{redirectUri}&popup=1") : ($"{redirectUri}?popup=1");
                                var authorizeUri = _dropboxOAuthService.BuildAuthorizeUri(appKey!, redirectUri, state, pkce.CodeChallenge);
                                _logger.LogInformation("UpdateProfile AJAX requires OAuth; returning authorizeUrl for user {UserId} trace {TraceId}", userId, HttpContext.TraceIdentifier);
                                return Json(new { success = false, requiresOAuth = true, authorizeUrl = authorizeUri.ToString() });
                            }
                            // Non-AJAX fallback
                            return StatusCode(428, "Dropbox connection required");
                        }

                        _logger.LogError("Dropbox token resolution failed while updating profile for user {UserId} trace {TraceId}: {Error}", userId, HttpContext.TraceIdentifier, tokenResult.Error ?? "Unknown error");
                        if (isAjax) return StatusCode(500, new { success = false, toastType = "error", toastMessage = "An unexpected error occurred", message = "An unexpected error occurred" });
                        return StatusCode(500, "An unexpected error occurred");
                    }

                    try
                    {
                        var profilePic = form.Files["profilePicture"];
                        var directUrl = await _dropboxPictureService.UploadUserProfilePictureAsync(
                            profilePic,
                            userId.Value.ToString(),
                            tokenResult.AccessToken!);
                        user.ProfilePictureUrl = directUrl;
                    }
                    catch (ActioNator.Services.Exceptions.FileStorageException fse) when (ExceptionIndicatesExpiredToken(fse))
                    {
                        if (tokenResult.UsedSharedToken)
                        {
                            _logger.LogError(fse, "Expired Dropbox shared token during UpdateProfile for user {UserId} trace {TraceId}", userId, HttpContext.TraceIdentifier);
                            var msg = "Dropbox shared credentials are invalid or expired. Please update the configured SharedRefreshToken (or SharedAccessToken).";
                            if (isAjax) return StatusCode(502, new { success = false, toastType = "error", toastMessage = msg, message = msg });
                            return StatusCode(502, msg);
                        }

                        _logger.LogWarning(fse, "Expired Dropbox access token during UpdateProfile for user {UserId}; retrying with refreshed token. trace {TraceId}", userId, HttpContext.TraceIdentifier);
                        var retryToken = await _dropboxTokenProvider.GetAccessTokenAsync(userId.Value);
                        if (retryToken.Success && !retryToken.UsedSharedToken && !string.IsNullOrEmpty(retryToken.AccessToken))
                        {
                            var profilePic = form.Files["profilePicture"];
                            var directUrl = await _dropboxPictureService.UploadUserProfilePictureAsync(
                                profilePic,
                                userId.Value.ToString(),
                                retryToken.AccessToken!);
                            user.ProfilePictureUrl = directUrl;
                        }
                        else
                        {
                            var msg = "Failed to obtain a valid Dropbox token.";
                            if (isAjax) return StatusCode(502, new { success = false, toastType = "error", toastMessage = msg, message = msg });
                            return StatusCode(502, msg);
                        }
                    }
                    catch (ActioNator.Services.Exceptions.FileStorageException fse) when (ExceptionIndicatesMissingScope(fse))
                    {
                        _logger.LogError(fse, "Dropbox scope missing during UpdateProfile for user {UserId} trace {TraceId}", userId, HttpContext.TraceIdentifier);
                        var msg = "Dropbox app is missing required permissions. Enable 'files.content.write' (and sharing.read/sharing.write) in the Dropbox App Console, then re-authorize and update the SharedRefreshToken.";
                        if (isAjax) return StatusCode(502, new { success = false, toastType = "error", toastMessage = msg, message = msg });
                        return StatusCode(502, msg);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Dropbox upload failed during UpdateProfile for user {UserId} trace {TraceId}", userId, HttpContext.TraceIdentifier);
                        if (isAjax) return StatusCode(502, new { success = false, toastType = "error", toastMessage = "The connection to Dropbox was unsuccessful", message = "The connection to Dropbox was unsuccessful" });
                        return StatusCode(502, "The connection to Dropbox was unsuccessful");
                    }
                }

                // Update profile data stored in JSON
                string updatedHeadline = null;
                string finalCoverPhotoUrl = null;
                bool aboutUpdated = false;
                await _userProfileService.UpdateProfileDataAsync(userId.Value, profileData =>
                {
                    var headlineInput = form["headline"].FirstOrDefault();
                    if (headlineInput != null)
                    {
                        profileData.Headline = headlineInput;
                        updatedHeadline = headlineInput;
                    }
                    else
                    {
                        // Preserve and return the existing headline when no field provided
                        updatedHeadline = profileData.Headline;
                    }

                    var locationInput = form["location"].FirstOrDefault();
                    if (locationInput != null)
                    {
                        profileData.Location = locationInput;
                    }
                    
                    if (aboutSanitized != null)
                    {
                        profileData.AboutText = aboutSanitized;
                        aboutUpdated = true;
                    }
                    
                    if (!string.IsNullOrEmpty(coverPhotoUrl))
                    {
                        finalCoverPhotoUrl = $"/User/UserProfile/GetFile?filePath={Uri.EscapeDataString(coverPhotoUrl)}";
                        profileData.CoverPhotoUrl = finalCoverPhotoUrl;
                    }
                });
                
                // Save changes to user
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToArray();
                    if (isAjax) return BadRequest(new { success = false, validationErrors = new { general = errors }, toastType = "error", toastMessage = "Please fix the validation errors" });
                    return BadRequest(string.Join(", ", errors));
                }

                if (isAjax) return Ok(new {
                    success = true,
                    toastType = "success",
                    toastMessage = "Profile updated successfully!",
                    redirectUrl = (string?)null,
                    messages = Array.Empty<string>(),
                    profilePictureUrl = user.ProfilePictureUrl,
                    coverPhotoUrl = finalCoverPhotoUrl,
                    fullName = string.Join(" ", new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    headline = updatedHeadline,
                    aboutUpdated = aboutUpdated
                });
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating profile for user {UserId} trace {TraceId}", GetUserId(), HttpContext.TraceIdentifier);
                if (isAjax) return StatusCode(500, new { success = false, toastType = "error", toastMessage = "An unexpected error occurred", message = "An unexpected error occurred" });
                return StatusCode(500, "An unexpected error occurred");
            }
        }

        private bool IsAjaxRequest()
        {
            var xrw = Request.Headers["X-Requested-With"].ToString();
            if (!string.IsNullOrEmpty(xrw) && string.Equals(xrw, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            var accept = Request.Headers["Accept"].ToString();
            return !string.IsNullOrEmpty(accept) && accept.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePictureDropbox(IFormFile profilePicture, string dropboxAccessToken, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            if (profilePicture == null || profilePicture.Length == 0)
                return BadRequest("No file provided.");

            try
            {
                // Reuse existing validation pipeline
                var validator = _fileValidatorFactory.GetValidatorForFile(profilePicture);
                var validationResult = await validator.ValidateAsync(profilePicture);
                if (!validationResult.IsValid)
                    return BadRequest(validationResult.ErrorMessage);

                var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                if (user == null)
                    return NotFound("User not found");

                // Get access token via centralized provider
                var tokenResult = await _dropboxTokenProvider.GetAccessTokenAsync(userId.Value, cancellationToken);
                if (!tokenResult.Success)
                {
                    if (tokenResult.RequiresUserConsent)
                    {
                        return BadRequest("Dropbox is not connected. Please connect your Dropbox account.");
                    }
                    return StatusCode(500, "Failed to obtain Dropbox access token.");
                }

                // Upload to Dropbox and get a direct-render URL
                var url = await _dropboxPictureService.UploadUserProfilePictureAsync(
                    profilePicture,
                    userId.Value.ToString(),
                    tokenResult.AccessToken!,
                    cancellationToken);

                user.ProfilePictureUrl = url;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

                return Ok(new { url });
            }
            catch (InvalidImageFormatException ex)
            {
                // Specific handling for image validation failures coming from Dropbox service
                return BadRequest(ex.Message);
            }
            catch (FileServiceException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while uploading the profile picture: {ex.Message}");
            }
        }

        /// <summary>
        /// OAuth 2.0 redirect URI endpoint for Dropbox. Processes the authorization code and stores tokens in session.
        /// </summary>
        [HttpGet]
        [IgnoreAntiforgeryToken]
        [Route("/signin-dropbox")]
        public async Task<IActionResult> DropboxCallback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery(Name = "error")] string error,
            [FromQuery(Name = "error_description")] string errorDescription,
            CancellationToken cancellationToken)
        {
            var isPopup = Request.Query.ContainsKey("popup");
            if (!string.IsNullOrEmpty(error))
            {
                if (isPopup)
                {
                    var htmlError = $"<!DOCTYPE html><html><body><script>(function(){{try{{if(window.opener){{window.opener.postMessage({{source:'dropbox-oauth',status:'error',message:'{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(error + ": " + errorDescription)}'}}, window.location.origin);}}}}catch(e){{}} window.close();}})();</script></body></html>";
                    return Content(htmlError, "text/html");
                }
                return BadRequest($"Dropbox OAuth error: {error}. {errorDescription}");
            }
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                if (isPopup)
                {
                    var htmlError = "<!DOCTYPE html><html><body><script>(function(){try{if(window.opener){window.opener.postMessage({source:'dropbox-oauth',status:'error',message:'Missing authorization code or state.'}, window.location.origin);}}catch(e){} window.close();})();</script></body></html>";
                    return Content(htmlError, "text/html");
                }
                return BadRequest("Missing authorization code or state.");
            }

            var expectedState = HttpContext.Session.GetString("Dropbox:State");
            if (string.IsNullOrEmpty(expectedState) || !FixedTimeEquals(expectedState, state))
            {
                if (isPopup)
                {
                    var htmlError = "<!DOCTYPE html><html><body><script>(function(){try{if(window.opener){window.opener.postMessage({source:'dropbox-oauth',status:'error',message:'Invalid OAuth state.'}, window.location.origin);}}catch(e){} window.close();})();</script></body></html>";
                    return Content(htmlError, "text/html");
                }
                return BadRequest("Invalid OAuth state.");
            }

            var codeVerifier = HttpContext.Session.GetString("Dropbox:CodeVerifier");
            if (string.IsNullOrWhiteSpace(codeVerifier))
            {
                if (isPopup)
                {
                    var htmlError = "<!DOCTYPE html><html><body><script>(function(){try{if(window.opener){window.opener.postMessage({source:'dropbox-oauth',status:'error',message:'Missing PKCE verifier in session.'}, window.location.origin);}}catch(e){} window.close();})();</script></body></html>";
                    return Content(htmlError, "text/html");
                }
                return BadRequest("Missing PKCE verifier in session.");
            }

            // Clear one-time values to prevent replay
            HttpContext.Session.Remove("Dropbox:State");
            HttpContext.Session.Remove("Dropbox:CodeVerifier");

            var appKey = _dropboxOptions.Value.AppKey;
            if (string.IsNullOrWhiteSpace(appKey))
            {
                return BadRequest("Dropbox AppKey is not configured.");
            }

            var redirectUri = _dropboxOptions.Value.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/signin-dropbox";

            try
            {
                var token = await _dropboxOAuthService.ExchangeCodeForTokenAsync(
                    code,
                    appKey!,
                    redirectUri,
                    codeVerifier!,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(token.AccessToken))
                {
                    if (isPopup)
                    {
                        var htmlError = "<!DOCTYPE html><html><body><script>(function(){try{if(window.opener){window.opener.postMessage({source:'dropbox-oauth',status:'error',message:'Failed to obtain Dropbox access token.'}, window.location.origin);}}catch(e){} window.close();})();</script></body></html>";
                        return Content(htmlError, "text/html");
                    }
                    return StatusCode(502, "Failed to obtain Dropbox access token.");
                }

                HttpContext.Session.SetString("Dropbox:AccessToken", token.AccessToken);
                if (!string.IsNullOrWhiteSpace(token.RefreshToken))
                {
                    HttpContext.Session.SetString("Dropbox:RefreshToken", token.RefreshToken);
                    try
                    {
                        var userId = GetUserId();
                        if (userId != null)
                        {
                            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                            if (user != null)
                            {
                                user.DropboxRefreshTokenEncrypted = _tokenProtector.Protect(token.RefreshToken);
                                await _userManager.UpdateAsync(user);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist Dropbox refresh token for user {UserId}", GetUserId());
                    }
                }

                // TODO: Persist refresh token per user securely if desired

                if (isPopup)
                {
                    var htmlOk = "<!DOCTYPE html><html><body><script>(function(){try{if(window.opener){window.opener.postMessage({source:'dropbox-oauth',status:'success'}, window.location.origin);}}catch(e){} window.close();})();</script></body></html>";
                    return Content(htmlOk, "text/html");
                }

                // Minimal page to display the refresh token for development copy-paste
                var rt = token.RefreshToken ?? string.Empty;
                var rtEsc = HtmlEncoder.Default.Encode(rt);
                var cmd = $"dotnet user-secrets set \"Dropbox:SharedRefreshToken\" \"{rtEsc}\"";
                var html = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Dropbox OAuth Success</title>" +
                           "<style>body{font-family:system-ui,-apple-system,Segoe UI,Roboto,Ubuntu,Arial,sans-serif;margin:24px;line-height:1.5} .box{background:#f6f8fa;border:1px solid #d0d7de;border-radius:6px;padding:12px;overflow-wrap:anywhere} code,textarea{font-family:ui-monospace,SFMono-Regular,Consolas,Monaco,monospace} textarea{width:100%;min-height:80px} .muted{color:#555}</style></head><body>" +
                           "<h2>Dropbox OAuth Success</h2>" +
                           "<p class=\"muted\">Copy the refresh token below and store it securely in user-secrets for development.</p>" +
                           "<h4>Refresh Token</h4>" +
                           $"<div class=\"box\"><textarea readonly>{rtEsc}</textarea></div>" +
                           "<h4>Set via user-secrets</h4>" +
                           $"<div class=\"box\"><code>{cmd}</code></div>" +
                           "<p class=\"muted\"><strong>Note:</strong> Do not commit this token. Use environment variables or secrets in production.</p>" +
                           "</body></html>";
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                if (isPopup)
                {
                    var htmlError = $"<!DOCTYPE html><html><body><script>(function(){{try{{if(window.opener){{window.opener.postMessage({{source:'dropbox-oauth',status:'error',message:'{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(ex.Message)}'}}, window.location.origin);}}}}catch(e){{}} window.close();}})();</script></body></html>";
                    return Content(htmlError, "text/html");
                }
                return StatusCode(500, $"OAuth callback processing failed: {ex.Message}");
            }
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);
            if (aBytes.Length != bBytes.Length) return false;
            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        /// <summary>
        /// Returns whether the current user has a stored Dropbox refresh token.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DropboxConnectionStatus()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            var connected = user != null && !string.IsNullOrWhiteSpace(user.DropboxRefreshTokenEncrypted);
            return Ok(new { connected });
        }

        /// <summary>
        /// Serves files from local storage
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File content</returns>
        [HttpGet]
        public async Task<IActionResult> GetFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return BadRequest("File path is required");
                
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();
                
            try
            {
                // Check if user is authorized to access this file
                bool isAuthorized = await _fileStorageService.IsUserAuthorizedForFileAsync(filePath, userId.Value.ToString());
                if (!isAuthorized)
                {
                    return Forbid();
                }
                
                // Get the file
                var (fileStream, contentType) = await _fileStorageService.GetFileAsync(filePath, userId.Value.ToString());
                
                // Return the file
                return File(fileStream, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound("File not found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the file: {ex.Message}");
            }
        }

        /// <summary>
        /// Test action to help with testing the UserProfile page
        /// This action gets the first user ID from the database and redirects to their profile
        /// </summary>
        /// <returns>Redirect to the profile page of the first user</returns>
        public IActionResult TestProfile()
        {
            // Use a hardcoded GUID for testing purposes
            Guid testUserId = new Guid("11111111-1111-1111-1111-111111111111");
            
            // Redirect to the profile page with the test user ID
            return RedirectToAction("Index", new { userId = testUserId });
        }
    }
}
