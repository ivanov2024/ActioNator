using System;

namespace ActioNator.ViewModels.Reports
{
    public class ReportedPostViewModel
    {
        public Guid PostId { get; set; }
        public string ContentPreview { get; set; }
        public string AuthorUserName { get; set; }
        public string ReportReason { get; set; }
        public string ReporterUserName { get; set; }
        public int TotalReports { get; set; }
        public DateTime ReportedAt { get; set; }
    }
}
