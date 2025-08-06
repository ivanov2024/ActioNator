using System;
using System.Collections.Generic;

namespace ActioNator.ViewModels.Admin
{
    public class ReportsViewModel
    {
        public List<PostReportViewModel> PostReports { get; set; } = new List<PostReportViewModel>();
        public List<CommentReportViewModel> CommentReports { get; set; } = new List<CommentReportViewModel>();
    }

    public class PostReportViewModel
    {
        public Guid Id { get; set; }
        public Guid PostId { get; set; }
        public Guid ReportedByUserId { get; set; }
        public string ReportedByUserName { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }

    public class CommentReportViewModel
    {
        public Guid Id { get; set; }
        public Guid CommentId { get; set; }
        public Guid PostId { get; set; }
        public string CommentContent { get; set; }
        public Guid ReportedByUserId { get; set; }
        public string ReportedByUserName { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }

    public class PostReportDetailsViewModel : PostReportViewModel
    {
        public string PostContent { get; set; }
        public string Details { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public string ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNotes { get; set; }
    }

    public class CommentReportDetailsViewModel : CommentReportViewModel
    {
        public string Details { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public string ReviewedByUserName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewNotes { get; set; }
    }
}
