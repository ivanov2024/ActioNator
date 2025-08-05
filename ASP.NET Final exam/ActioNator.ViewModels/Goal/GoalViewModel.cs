using ActioNator.GCommon;
using System.ComponentModel.DataAnnotations;

using static ActioNator.GCommon.ValidationConstants.Goal;

namespace ActioNator.ViewModels.Goal
{
    public class GoalViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(
            TitleMaxLength, 
            MinimumLength = TitleMinLength, 
            ErrorMessage = "Title must be between {2} and {1} characters")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(
            DescriptionMaxLength, 
            MinimumLength = DescriptionMinLength, 
            ErrorMessage = "Description must be between {2} and {1} characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Completed")]
        public bool Completed { get; set; }

        [Display(Name = "Overdue")]
        public bool IsOverdue => !Completed && DueDate < DateTime.Today;
    }
}
