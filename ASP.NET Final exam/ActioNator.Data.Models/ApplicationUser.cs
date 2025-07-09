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

        public virtual ICollection<Goal> Goals { get; set; } 
            = new HashSet<Goal>();

        public virtual ICollection<Workout> Workouts { get; set; }
            = new HashSet<Workout>();

        public virtual ICollection<Post> Posts { get; set; }
            = new HashSet<Post>();

        public virtual ICollection<Comment> Comments { get; set; }
            = new HashSet<Comment>();

        public virtual ICollection<JournalEntry> JournalEntries { get; set; }
            = new HashSet<JournalEntry>();

        public virtual ICollection<Achievement> Achievements { get; set; }
            = new HashSet<Achievement>();

        public virtual ICollection<Chat> Chats { get; set; }
            = new HashSet<Chat>();

        public virtual ICollection<Message> Messages { get; set; }
            = new HashSet<Message>();
    }
}
