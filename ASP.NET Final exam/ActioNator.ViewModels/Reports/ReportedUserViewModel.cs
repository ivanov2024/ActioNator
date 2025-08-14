using System;

namespace ActioNator.ViewModels.Reports
{
    public class ReportedUserViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string ReportReason { get; set; }
        public string ReporterUserName { get; set; }
        public int TotalReports { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
