using System;
using System.ComponentModel.DataAnnotations;
using static ActioNator.GCommon.ValidationConstants.JournalEntry;

namespace ActioNator.ViewModels.Journal
{
    public class JournalEntryViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = TitleRequiredMessage)]
        [StringLength(TitleMaxLength, MinimumLength = TitleMinLength, ErrorMessage = TitleLengthMessage)]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = ContentRequiredMessage)]
        [StringLength(ContentMaxLength, MinimumLength = ContentMinLength, ErrorMessage = ContentLengthMessage)]
        public string Content { get; set; } = null!;

        [Required(ErrorMessage = MoodTagRequiredMessage)]
        [StringLength(MoodTagMaxLength, MinimumLength = MoodTagMinLength, ErrorMessage = MoodTagLengthMessage)]
        public string? MoodTag { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
