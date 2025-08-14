using System;

namespace ActioNator.ViewModels.Community
{
    public class UserReportRequest
    {
        public string Reason { get; set; }
        public string? Details { get; set; }
    }
}
