using System;
using System.Collections.Generic;

namespace FinalExamUI.ViewModels.UserProfile
{
    public class AboutTabViewModel
    {
        /// <summary>
        /// Dropbox URL for the user's background image.
        /// </summary>
        public string? BackgroundImageUrl { get; set; }
        public Guid UserId { get; set; }
        public PersonalInfoSection PersonalInfo { get; set; } = new PersonalInfoSection();
        public ContactInfoSection ContactInfo { get; set; } = new ContactInfoSection();
        public List<string> Languages { get; set; } = new List<string>();
        public string RelationshipStatus { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }
        public Dictionary<string, bool> PrivacySettings { get; set; } = new Dictionary<string, bool>();
    }

    public class PersonalInfoSection
    {
        public string About { get; set; }
        public string Hometown { get; set; }
        public string CurrentCity { get; set; }
        public List<string> Hobbies { get; set; } = new List<string>();
        public List<string> FavoriteQuotes { get; set; } = new List<string>();
    }

    public class ContactInfoSection
    {
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public Dictionary<string, string> SocialProfiles { get; set; } = new Dictionary<string, string>();
    }
}
