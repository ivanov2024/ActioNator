using ActioNator.ViewModels.Journal;

namespace ActioNator.Services.Interfaces.JournalService
{
    /// <summary>
    /// Interface for journal entry management operations
    /// </summary>
    public interface IJournalService
    {
        /// <summary>
        /// Get all journal entries
        /// </summary>
        /// <returns>Collection of journal entries</returns>
        Task<IEnumerable<JournalEntryViewModel>> GetAllEntriesAsync();
        
        /// <summary>
        /// Get a journal entry by its ID
        /// </summary>
        /// <param name="id">The journal entry ID</param>
        /// <returns>The journal entry if found, otherwise null</returns>
        Task<JournalEntryViewModel?> GetEntryByIdAsync(Guid id);
        
        /// <summary>
        /// Create a new journal entry
        /// </summary>
        /// <param name="entry">The journal entry to create</param>
        /// <returns>The created journal entry with assigned ID</returns>
        Task<JournalEntryViewModel> CreateEntryAsync(JournalEntryViewModel entry, Guid? userId);
        
        /// <summary>
        /// Update an existing journal entry
        /// </summary>
        /// <param name="entry">The journal entry to update</param>
        /// <returns>The updated journal entry</returns>
        Task<JournalEntryViewModel> UpdateEntryAsync(JournalEntryViewModel entry);
        
        /// <summary>
        /// Delete a journal entry by its ID
        /// </summary>
        /// <param name="id">The ID of the journal entry to delete</param>
        /// <returns>True if deletion was successful, otherwise false</returns>
        Task<bool> DeleteEntryAsync(Guid id);
        
        /// <summary>
        /// Search for journal entries by keyword
        /// </summary>
        /// <param name="searchTerm">The search term to find in title, content, or mood tag</param>
        /// <returns>Collection of matching journal entries</returns>
        Task<IEnumerable<JournalEntryViewModel>> SearchEntriesAsync(string searchTerm);
    }
}
