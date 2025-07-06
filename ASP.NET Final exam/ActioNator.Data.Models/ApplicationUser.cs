using Microsoft.AspNetCore.Identity;

using ActioNator.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.Data.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Role Role { get; set; }

        [Required]
        public bool IsVerifiedCoach { get; set; }

        public string? CoachDegreeFilePath { get; set; }

        [Required]
        public string ProfilePictureUrl { get; set; } = null!;

        public DateTime RegisteredAt { get; set; }

        public bool IsDeleted { get; set; }
    }
}
