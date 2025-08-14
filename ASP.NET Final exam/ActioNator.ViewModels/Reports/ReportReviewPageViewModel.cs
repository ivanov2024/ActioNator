using System.Collections.Generic;

namespace ActioNator.ViewModels.Reports
{
    public class ReportReviewPageViewModel
    {
        public List<ReportedPostViewModel> ReportedPosts { get; set; }
        public List<ReportedCommentViewModel> ReportedComments { get; set; }
        public List<ReportedUserViewModel> ReportedUsers { get; set; }
    }
}
