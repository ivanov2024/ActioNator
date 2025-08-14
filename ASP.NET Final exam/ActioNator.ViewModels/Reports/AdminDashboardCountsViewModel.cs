using System;

namespace ActioNator.ViewModels.Reports
{
    /// <summary>
    /// Aggregated counts for the Admin Home dashboard.
    /// </summary>
    public class AdminDashboardCountsViewModel
    {
        /// <summary>
        /// Number of coaches currently awaiting verification.
        /// </summary>
        public int PendingCoachVerifications { get; set; }

        /// <summary>
        /// Number of distinct posts with pending reports.
        /// </summary>
        public int PendingPostReports { get; set; }

        /// <summary>
        /// Number of distinct comments with pending reports.
        /// </summary>
        public int PendingCommentReports { get; set; }

        /// <summary>
        /// Number of users with pending reports (if supported).
        /// </summary>
        public int PendingUserReports { get; set; }
    }
}
