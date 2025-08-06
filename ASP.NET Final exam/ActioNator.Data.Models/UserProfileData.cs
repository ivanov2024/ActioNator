using System.Text.Json.Serialization;

namespace ActioNator.Data.Models
{
    public class UserProfileData
    {
        // Basic Info
        public string? Headline { get; set; }
        public string? Location { get; set; }
        
        // Media
        public string? CoverPhotoUrl { get; set; }
        
        // About Section
        public string? Bio { get; set; }
        public string? Interests { get; set; }
        
        // Contact Info
        public string? Website { get; set; }
        public string? SocialMediaLinks { get; set; }
        
        // Settings
        public bool ShowEmail { get; set; } = false;
        public bool ShowActivityStatus { get; set; } = true;
        
        [JsonIgnore]
        public bool IsEmpty => 
            string.IsNullOrWhiteSpace(Headline) &&
            string.IsNullOrWhiteSpace(Location) &&
            string.IsNullOrWhiteSpace(CoverPhotoUrl) &&
            string.IsNullOrWhiteSpace(Bio) &&
            string.IsNullOrWhiteSpace(Interests) &&
            string.IsNullOrWhiteSpace(Website) &&
            string.IsNullOrWhiteSpace(SocialMediaLinks);
    }
}
