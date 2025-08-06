using System;

namespace ActioNator.Data.Models
{
    /// <summary>
    /// Represents a user's like on a comment
    /// </summary>
    public class CommentLike
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the comment that was liked
        /// </summary>
        public Guid CommentId { get; set; }

        /// <summary>
        /// Reference to the comment
        /// </summary>
        public Comment Comment { get; set; } = null!;

        /// <summary>
        /// ID of the user who liked the comment
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Reference to the user
        /// </summary>
        public ApplicationUser ApplicationUser { get; set; } = null!;

        /// <summary>
        /// When the like was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Whether the like is active or removed
        /// </summary>
        public bool IsActive { get; set; }
    }
}
