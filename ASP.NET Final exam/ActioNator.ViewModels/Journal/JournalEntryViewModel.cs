using System;
using System.ComponentModel.DataAnnotations;

namespace ActioNator.ViewModels.Journal
{
    public class JournalEntryViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "The Title must be with length from 3 to 20. You currently have {0} characters.")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Content is required")]
        [StringLength(150, ErrorMessage = "The Content must be with length from 0 to 150. You currently have {0} characters.")]
        public string Content { get; set; } = null!;

        [StringLength(50, ErrorMessage = "The Mood Tag must be with length from 0 to 50. You currently have {0} characters.")]
        public string? MoodTag { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
