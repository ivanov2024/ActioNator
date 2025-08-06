using System;
using System.Threading.Tasks;
using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.UserProfileService;
using FinalExamUI.ViewModels.UserProfile;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ActioNator.Services.Interfaces.FileServices;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    public class UserProfileController : BaseController
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileValidatorFactory _fileValidatorFactory;

        public UserProfileController(
            IUserProfileService userProfileService,
            UserManager<ApplicationUser> userManager,
            IFileStorageService fileStorageService,
            IFileValidatorFactory fileValidatorFactory)
            : base(userManager)
        {
            _userProfileService = userProfileService;
            _fileStorageService = fileStorageService;
            _fileValidatorFactory = fileValidatorFactory;
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

            return View(profileViewModel);
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
        /// Handles updating the user's profile information
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

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
                        return BadRequest(validationResult.ErrorMessage);

                    // Save profile picture to App_Data
                    profilePictureUrl = (await _fileStorageService.SaveFilesAsync(
                        new FormFileCollection { profilePic }, 
                        "App_Data/profile-pictures", 
                        userId.Value.ToString())).FirstOrDefault();
                }

                // Handle cover photo upload if provided
                string coverPhotoUrl = null;
                if (form.Files["coverPhoto"] != null && form.Files["coverPhoto"].Length > 0)
                {
                    var coverPhoto = form.Files["coverPhoto"];
                    var validator = _fileValidatorFactory.GetValidatorForFile(coverPhoto);
                    var validationResult = await validator.ValidateAsync(coverPhoto);
                    if (!validationResult.IsValid)
                        return BadRequest(validationResult.ErrorMessage);

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

                // Update basic user properties
                user.FirstName = form["firstName"].FirstOrDefault() ?? user.FirstName;
                user.LastName = form["lastName"].FirstOrDefault() ?? user.LastName;
                
                if (!string.IsNullOrEmpty(profilePictureUrl))
                    user.ProfilePictureUrl = $"/User/UserProfile/GetFile?filePath={Uri.EscapeDataString(profilePictureUrl)}";

                // Update profile data stored in JSON
                await _userProfileService.UpdateProfileDataAsync(userId.Value, profileData =>
                {
                    profileData.Headline = form["headline"].FirstOrDefault();
                    profileData.Location = form["location"].FirstOrDefault();
                    
                    if (!string.IsNullOrEmpty(coverPhotoUrl))
                        profileData.CoverPhotoUrl = $"/User/UserProfile/GetFile?filePath={Uri.EscapeDataString(coverPhotoUrl)}";
                });
                
                // Save changes to user
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));

                return Ok();
            }
            catch (Exception ex)
            {
                // Log the error
                return StatusCode(500, $"An error occurred while updating the profile: {ex.Message}");
            }
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
