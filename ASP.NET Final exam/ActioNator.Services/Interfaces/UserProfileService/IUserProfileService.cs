using System;
using System.Threading.Tasks;
using FinalExamUI.ViewModels.UserProfile;
using ActioNator.Data.Models;

namespace ActioNator.Services.Interfaces.UserProfileService
{
    public interface IUserProfileService
    {
        /// <summary>
        /// Gets the complete user profile data for the specified user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A ProfileViewModel containing all profile data</returns>
        Task<ProfileViewModel> GetUserProfileAsync(Guid userId);
        
        /// <summary>
        /// Gets data for the Friends tab of a user's profile
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>A FriendsTabViewModel containing friends data</returns>
        Task<FriendsTabViewModel> GetFriendsTabAsync(Guid userId);
        
        /// <summary>
        /// <returns>Task representing the asynchronous operation</returns>
        Task UpdateAboutTabAsync(Guid userId, AboutTabViewModel aboutTabViewModel);

        /// <summary>
        /// Gets the additional profile data for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>The user's profile data</returns>
        Task<UserProfileData> GetProfileDataAsync(Guid userId);

        /// <summary>
        /// Updates the additional profile data for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="updateAction">Action to update the profile data</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task UpdateProfileDataAsync(Guid userId, Action<UserProfileData> updateAction);
    }
}
