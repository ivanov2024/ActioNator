using System;

namespace ActioNator.ViewModels.Journal
{
    // DTO used for JSON delete requests with antiforgery token extracted by custom attribute
    public class DeleteJournalRequest
    {
        public Guid Id { get; set; }
        public string? __RequestVerificationToken { get; set; }
    }
}
