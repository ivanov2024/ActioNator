using System;

namespace ActioNator.Data.Models
{
    /// <summary>
    /// Represents a user's like on a post
    /// </summary>
    public class PostLike
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the post that was liked
        /// </summary>
        public Guid PostId { get; set; }

        /// <summary>
        /// Reference to the post
        /// </summary>
        public Post Post { get; set; }

        /// <summary>
        /// ID of the user who liked the post
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
