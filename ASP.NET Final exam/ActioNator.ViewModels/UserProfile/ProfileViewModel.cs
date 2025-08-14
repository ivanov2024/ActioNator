using System;
using System.Collections.Generic;

namespace FinalExamUI.ViewModels.UserProfile
{
    public class ProfileViewModel
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string CoverPhotoUrl { get; set; }
        public string Headline { get; set; }
        public string Location { get; set; }
        public string AboutText { get; set; }
        public int FriendsCount { get; set; }
        public bool IsCurrentUser { get; set; }
        public string ActiveTab { get; set; } = "Overview";
        
        // Navigation properties for tabs
        public OverviewTabViewModel Overview { get; set; }
        public AboutTabViewModel About { get; set; }
        public FriendsTabViewModel Friends { get; set; }

        // Indicates whether this user is a verified coach
        public bool IsVerifiedCoach { get; set; }

        // Indicates whether this user is an administrator
        public bool IsAdmin { get; set; }
    }
}
