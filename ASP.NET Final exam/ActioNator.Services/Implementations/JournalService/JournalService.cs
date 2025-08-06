using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.JournalService;
using ActioNator.ViewModels.Journal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActioNator.Services.Implementations.JournalService
{
    /// <summary>
    /// Implementation of the journal service that manages journal entries
    /// </summary>
    public class JournalService : IJournalService
    {
        private readonly ActioNatorDbContext _dbContext;

        public JournalService(ActioNatorDbContext dbContext)
        {
            _dbContext = dbContext
                ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Get all journal entries
        /// </summary>
        public async Task<IEnumerable<JournalEntryViewModel>> GetAllEntriesAsync()
        => await _dbContext
            .JournalEntries
            .AsNoTracking()
            .Select(je => new JournalEntryViewModel
            {
                Id = je.Id,
                Title = je.Title,
                Content = je.Content,
                MoodTag = je.MoodTag,
                CreatedAt = je.CreatedAt
            })
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();


        /// <summary>
        /// Get a journal entry by its ID
        /// </summary>
        public async Task<JournalEntryViewModel?> GetEntryByIdAsync(Guid id)
            => await
            _dbContext
            .JournalEntries
            .AsNoTracking()
            .Select(je => new JournalEntryViewModel
            {
                Id = je.Id,
                Title = je.Title,
                Content = je.Content,
                MoodTag = je.MoodTag,
                CreatedAt = je.CreatedAt
            })
            .FirstOrDefaultAsync(je => je.Id == id);

        /// <summary>
        /// Create a new journal entry
        /// </summary>
        public async Task<JournalEntryViewModel> CreateEntryAsync(JournalEntryViewModel entry, Guid? userId)
        {
            if (string.IsNullOrEmpty(entry.Title) || entry.Title.Length < 3 || entry.Title.Length > 20)
            {
                throw new ArgumentException("Title must be between 3 and 20 characters.");
            }

            if (entry.Content?.Length > 150)
            {
                throw new ArgumentException("Content must be 150 characters or less.");
            }

            if (entry.MoodTag?.Length > 50)
            {
                throw new ArgumentException("Mood tag must be 50 characters or less.");
            }

            JournalEntry journalEntry = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId!.Value,
                Title = entry.Title,
                Content = entry.Content!,
                MoodTag = entry.MoodTag,
                CreatedAt = DateTime.UtcNow
            };

            // Add the JournalEntry entity to collection (not the ViewModel)
            await _dbContext.JournalEntries
                .AddAsync(journalEntry);

            await _dbContext
                .SaveChangesAsync();

            // Map the entity back to view model for return
            entry.Id = journalEntry.Id;
            entry.CreatedAt = journalEntry.CreatedAt;
            
            return entry;
        }

        /// <summary>
        /// Update an existing journal entry
        /// </summary>
        public async Task<JournalEntryViewModel> UpdateEntryAsync(JournalEntryViewModel entry)
        {
            // Validate entry
            if (string.IsNullOrEmpty(entry.Title) || entry.Title.Length < 3 || entry.Title.Length > 20)
            {
                throw new ArgumentException("Title must be between 3 and 20 characters.");
            }

            if (entry.Content?.Length > 150)
            {
                throw new ArgumentException("Content must be 150 characters or less.");
            }

            if (entry.MoodTag?.Length > 50)
            {
                throw new ArgumentException("Mood tag must be 50 characters or less.");
            }

            JournalEntry? existingEntry
                = await _dbContext
                .JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entry.Id)
                ?? throw new KeyNotFoundException($"Journal entry with ID {entry.Id} not found.");

            existingEntry.Title = entry.Title;
            existingEntry.Content = entry.Content!;
            existingEntry.MoodTag = entry.MoodTag;

            await _dbContext.SaveChangesAsync();

            return new JournalEntryViewModel
            {
                Id = existingEntry.Id,
                Title = existingEntry.Title,
                Content = existingEntry.Content,
                MoodTag = existingEntry.MoodTag,
                CreatedAt = existingEntry.CreatedAt
            };
        }

        /// <summary>
        /// Delete a journal entry by its ID
        /// </summary>
        public async Task<bool> DeleteEntryAsync(Guid id)
        {
            JournalEntry? entry
                = await
                _dbContext
                .JournalEntries
                .Where(je => je.Id == id)
                .FirstOrDefaultAsync();

            if (entry == null)
            {
                return false;
            }

            entry.IsDeleted = true;

            await _dbContext
                .SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Search for journal entries by keyword
        /// </summary>
        public async Task<IEnumerable<JournalEntryViewModel>> SearchEntriesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllEntriesAsync();
            }

            string normalizedSearchTerm
                = searchTerm.ToLower();

            var results = 
                await 
                _dbContext
                .JournalEntries
                .Where(e =>
                    e.Title.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    e.Content.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    (e.MoodTag != null 
                    && e.MoodTag.Contains(normalizedSearchTerm, StringComparison.CurrentCultureIgnoreCase)))
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new JournalEntryViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Content = e.Content,
                    MoodTag = e.MoodTag,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return results;
        }
    }
}
