using System;

namespace ActioNator.ViewModels.Journal
{
    /// <summary>
    /// Request model for deleting a journal entry
    /// </summary>
    public class DeleteEntryRequest
    {
        /// <summary>
        /// The ID of the journal entry to delete
        /// </summary>
        public Guid Id { get; set; }
    }
}
