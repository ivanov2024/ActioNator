using System;

namespace ActioNator.Data.Models
{
    /// <summary>
    /// Represents a user report on a post
    /// </summary>
    public class PostReport
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the post that was reported
        /// </summary>
        public Guid PostId { get; set; }

        /// <summary>
        /// Reference to the post
        /// </summary>
        public Post Post { get; set; }

        /// <summary>
        /// ID of the user who reported the post
        /// </summary>
        public Guid ReportedByUserId { get; set; }

        /// <summary>
        /// Reference to the user who reported the post
        /// </summary>
        public ApplicationUser ReportedByUser { get; set; }

        /// <summary>
        /// Reason for the report
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Additional details provided by the reporting user
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// When the report was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Status of the report (e.g., Pending, Reviewed, Resolved)
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// ID of the admin user who reviewed the report (if any)
        /// </summary>
        public Guid? ReviewedByUserId { get; set; }

        /// <summary>
        /// Reference to the admin user who reviewed the report
        /// </summary>
        public ApplicationUser ReviewedByUser { get; set; }

        /// <summary>
        /// When the report was reviewed
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// Notes added by the reviewer
        /// </summary>
        public string ReviewNotes { get; set; }
    }
}
