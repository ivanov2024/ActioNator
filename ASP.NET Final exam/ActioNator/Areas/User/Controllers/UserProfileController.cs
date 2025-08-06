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
        private readonly IDropboxFileStorageService _dropboxFileStorageService;
        private readonly IFileValidatorFactory _fileValidatorFactory;

        public UserProfileController(
            IUserProfileService userProfileService,
            UserManager<ApplicationUser> userManager,
            IDropboxFileStorageService dropboxFileStorageService,
            IFileValidatorFactory fileValidatorFactory)
            : base(userManager)
        {
            _userProfileService = userProfileService;
            _dropboxFileStorageService = dropboxFileStorageService;
            _fileValidatorFactory = fileValidatorFactory;
        }

        [HttpPost]
        public async Task<IActionResult> UploadAboutBackgroundImage(Guid userId, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No file uploaded.");

            var validator = _fileValidatorFactory.GetValidatorForFile(image);
            var validationResult = await validator.ValidateAsync(image);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.ErrorMessage);

            var url = (await _dropboxFileStorageService.SaveFilesAsync(new FormFileCollection { image }, "about-backgrounds", userId.ToString())).FirstOrDefault();
            if (string.IsNullOrEmpty(url))
                return StatusCode(500, "Failed to upload image to Dropbox.");

            var aboutTab = await _userProfileService.GetAboutTabAsync(userId);
            aboutTab.BackgroundImageUrl = url;
            await _userProfileService.UpdateAboutTabAsync(userId, aboutTab);
            return Ok(new { imageUrl = url });
        }

        [HttpPost]
        public async Task<IActionResult> ReplaceAboutBackgroundImage(Guid userId, IFormFile image)
        {
            var aboutTab = await _userProfileService.GetAboutTabAsync(userId);
            if (!string.IsNullOrEmpty(aboutTab.BackgroundImageUrl))
                await _dropboxFileStorageService.DeleteFileAsync(aboutTab.BackgroundImageUrl);
            return await UploadAboutBackgroundImage(userId, image);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAboutBackgroundImage(Guid userId)
        {
            var aboutTab = await _userProfileService.GetAboutTabAsync(userId);
            if (!string.IsNullOrEmpty(aboutTab.BackgroundImageUrl))
            {
                await _dropboxFileStorageService.DeleteFileAsync(aboutTab.BackgroundImageUrl);
                aboutTab.BackgroundImageUrl = null;
                await _userProfileService.UpdateAboutTabAsync(userId, aboutTab);
            }
            return Ok();
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
        /// Returns the Overview tab partial view
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>The Overview partial view</returns>
        public async Task<IActionResult> GetOverviewPartial(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid guid))
            {
                return NotFound();
            }

            var viewModel = await _userProfileService.GetOverviewTabAsync(guid);
            
            if (viewModel == null)
            {
                return NotFound();
            }

            return PartialView("_OverviewPartial", viewModel);
        }

        /// <summary>
        /// Returns the About tab partial view
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>The About partial view</returns>
        public async Task<IActionResult> GetAboutPartial(string userId)
        {
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out Guid guid))
            {
                return NotFound();
            }

            var viewModel = await _userProfileService.GetAboutTabAsync(guid);
            
            if (viewModel == null)
            {
                return NotFound();
            }

            return PartialView("_AboutPartial", viewModel);
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
        /// Updates the About tab data for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="model">The updated About tab data</param>
        /// <returns>JSON result indicating success or failure</returns>
        [HttpPost]
        public async Task<IActionResult> UpdateAboutPartial(string userId, [FromBody] AboutTabViewModel model)
        {
            if (string.IsNullOrEmpty(userId) || model == null || !Guid.TryParse(userId, out Guid guid))
            {
                return BadRequest("Invalid user ID or About data");
            }
            
            try
            {
                // Ensure the model's UserId matches the route userId
                model.UserId = guid;
                
                // Update the About tab data
                await _userProfileService
                    .UpdateAboutTabAsync(guid, model);
                
                return Json(new { success = true, message = "About information updated successfully" });
            }
            catch (Exception ex)
            {
                // In a real application, you would log the error
                return Json(new { success = false, message = $"Error updating About information: {ex.Message}" });
            }
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

                    profilePictureUrl = (await _dropboxFileStorageService.SaveFilesAsync(
                        new FormFileCollection { profilePic }, 
                        "profile-pictures", 
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

                    coverPhotoUrl = (await _dropboxFileStorageService.SaveFilesAsync(
                        new FormFileCollection { coverPhoto }, 
                        "cover-photos", 
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
                    user.ProfilePictureUrl = profilePictureUrl;

                // Update profile data stored in JSON
                await _userProfileService.UpdateProfileDataAsync(userId.Value, profileData =>
                {
                    profileData.Headline = form["headline"].FirstOrDefault();
                    profileData.Location = form["location"].FirstOrDefault();
                    
                    if (!string.IsNullOrEmpty(coverPhotoUrl))
                        profileData.CoverPhotoUrl = coverPhotoUrl;
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
