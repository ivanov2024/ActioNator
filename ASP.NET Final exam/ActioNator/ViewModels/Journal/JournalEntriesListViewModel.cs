using System;
using System.Collections.Generic;

namespace ActioNator.ViewModels.Journal
{
    public class JournalEntriesListViewModel
    {
        public List<JournalEntryViewModel> Entries { get; set; } = new List<JournalEntryViewModel>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 3;
        public int TotalCount { get; set; } = 0;
        public int TotalPages { get; set; } = 1;
        public string? SearchTerm { get; set; }
    }
}
