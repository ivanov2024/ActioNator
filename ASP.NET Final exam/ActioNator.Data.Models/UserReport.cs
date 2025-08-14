using System;

namespace ActioNator.Data.Models
{
    /// <summary>
    /// Represents a user report on another user
    /// </summary>
    public class UserReport
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID of the user that was reported
        /// </summary>
        public Guid ReportedUserId { get; set; }

        /// <summary>
        /// Reference to the reported user
        /// </summary>
        public ApplicationUser ReportedUser { get; set; }

        /// <summary>
        /// ID of the user who submitted the report
        /// </summary>
        public Guid ReportedByUserId { get; set; }

        /// <summary>
        /// Reference to the user who submitted the report
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
