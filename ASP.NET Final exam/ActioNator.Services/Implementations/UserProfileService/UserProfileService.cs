using System;
using System.IO;
using ActioNator.Data;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.FileServices;
using ActioNator.Services.Interfaces.UserProfileService;
using FinalExamUI.ViewModels.UserProfile;
using System.Text.Json;

namespace ActioNator.Services.Implementations.UserProfileService
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ActioNatorDbContext _dbContext;

        public UserProfileService(IFileSystem fileSystem, IWebHostEnvironment webHostEnvironment, ActioNatorDbContext dbContext)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        
        /// <summary>
        /// Gets the complete user profile data for the specified user
        /// </summary>
        public async Task<ProfileViewModel> GetUserProfileAsync(Guid userId)
        {
            // Fetch the user from the database
            var user = await _dbContext.ApplicationUsers.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            // Get additional profile data from JSON
            var profileData = await GetProfileDataAsync(userId);

            var profile = new ProfileViewModel
            {
                UserId = user.Id,
                FullName = $"{user.FirstName} {user.LastName}",
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CoverPhotoUrl = profileData?.CoverPhotoUrl ?? string.Empty,
                Headline = profileData?.Headline ?? string.Empty,
                Location = profileData?.Location ?? string.Empty,
                AboutText = profileData?.AboutText ?? string.Empty,
                FriendsCount = 0, // We removed friends functionality
                IsCurrentUser = false, // Set this based on context if needed
                ActiveTab = "Overview",
                IsVerifiedCoach = user.IsVerifiedCoach
            };

            // Load data for each tab
            profile.Friends = await GetFriendsTabAsync(userId);

            return profile;
        }
        
        /// <summary>
        /// Gets data for the Friends tab of a user's profile
        /// </summary>
        public async Task<FriendsTabViewModel> GetFriendsTabAsync(Guid userId)
        {
            var user = await _dbContext.ApplicationUsers.FindAsync(userId);
            if (user == null)
            {
                return null;
            }

            var friends = new List<FriendItem>();

            return new FriendsTabViewModel
            {
                UserId = userId,
                TotalFriendsCount = 0,
                Friends = friends,
                MutualFriends = new List<FriendItem>(),
                FriendSuggestions = new List<FriendItem>(),
                FriendsByLocation = new Dictionary<string, int>()
            };
        }
        

        /// <summary>
        /// Saves the About tab data to a JSON file in the App_Data folder
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="aboutTabViewModel">The About tab data to save</param>
        /// <returns>Task representing the asynchronous operation</returns>
        
        
        /// <summary>
        /// Updates the About tab data for a user
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <param name="aboutTabViewModel">The updated About tab data</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateAboutTabAsync(Guid userId, AboutTabViewModel aboutTabViewModel)
        {
            var user = await _dbContext.ApplicationUsers.FindAsync(userId);
            if (user == null || aboutTabViewModel == null)
            {
                throw new ArgumentException("Invalid user ID or About tab data");
            }

            // Update user properties based on aboutTabViewModel
            // Persist AboutTabViewModel as JSON file per user
            var aboutTabJsonPath = Path.Combine("UserData", "AboutTabs", $"about_{userId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(aboutTabJsonPath));
            var aboutTabJson = System.Text.Json.JsonSerializer.Serialize(aboutTabViewModel, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(aboutTabJsonPath, aboutTabJson);
        }
        /// <summary>
        /// Gets the additional profile data for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>The user's profile data</returns>
        public async Task<UserProfileData> GetProfileDataAsync(Guid userId)
        {
            var profileDataJsonPath = Path.Combine("UserData", "ProfileData", $"profile_{userId}.json");
            if (System.IO.File.Exists(profileDataJsonPath))
            {
                var profileDataJson = await System.IO.File.ReadAllTextAsync(profileDataJsonPath);
                var profileData = System.Text.Json.JsonSerializer.Deserialize<UserProfileData>(profileDataJson);
                if (profileData != null)
                {
                    return profileData;
                }
            }
            // If no JSON exists, return a new UserProfileData with the userId
            return new UserProfileData();
        }

        /// <summary>
        /// Updates the additional profile data for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="updateAction">Action to update the profile data</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task UpdateProfileDataAsync(Guid userId, Action<UserProfileData> updateAction)
        {
            var profileData = await GetProfileDataAsync(userId) ?? new UserProfileData();
            updateAction(profileData);
            var profileDataJsonPath = Path.Combine("UserData", "ProfileData", $"profile_{userId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(profileDataJsonPath));
            var profileDataJson = System.Text.Json.JsonSerializer.Serialize(profileData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(profileDataJsonPath, profileDataJson);
        }
    }
}
