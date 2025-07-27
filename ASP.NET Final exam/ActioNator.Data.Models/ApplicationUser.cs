using Microsoft.AspNetCore.Identity;

using ActioNator.Data.Models.Enums;
using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.ApplicationUser;

namespace ActioNator.Data.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        [Required]
        [MinLength(FirstNameMinLength)]
        [MaxLength(FirstNameMaxLength)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MinLength(LastNameMinLength)]
        [MaxLength(LastNameMaxLength)]
        public string LastName { get; set; } = null!;

        public Role Role { get; set; }

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

        /// <summary>
        /// Gets or sets the collection of chats where the user is the first participant (initiator).
        /// </summary>
        public ICollection<Chat> ChatsInitiated { get; set; } = new HashSet<Chat>();

        /// <summary>
        /// Gets or sets the collection of chats where the user is the second participant (receiver).
        /// </summary>
        public ICollection<Chat> ChatsReceived { get; set; } = new HashSet<Chat>();

        /// <summary>
        /// Gets or sets the collection of messages sent by the user.
        /// </summary>
        public ICollection<Message> MessagesSent { get; set; } = new HashSet<Message>();

        /// <summary>
        /// Gets or sets the collection of messages received by the user.
        /// </summary>
        public ICollection<Message> MessagesReceived { get; set; } = new HashSet<Message>();
    }
}
