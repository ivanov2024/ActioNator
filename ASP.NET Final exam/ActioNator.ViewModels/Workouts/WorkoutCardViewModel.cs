namespace ActioNator.ViewModels.Workouts
{
    public class WorkoutCardViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;

        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Display of the duration in minutes for easier reading (example: "45 mins")
        /// </summary>
        public string DurationDisplay 
            => $"{(int)Duration.TotalMinutes} mins";

        public DateTime? CompletedAt { get; set; } = null!;

        /// <summary>
        /// A formatted string for displaying the date (example: "25 Aug 2025")
        /// </summary>
        public string? CompletedDateDisplay 
            => CompletedAt?.ToString("dd MMM yyyy");
    }
}
