using ActioNator.ViewModels.Workouts;
using ActioNator.ViewModels.Posts;

namespace ActioNator.ViewModels.Dashboard
{
    public class DashboardViewModel
    {
        // User-related properties
        public string UserName { get; set; } = null!;
        
        // Count properties
        public int ActiveGoalsCount { get; set; }
        public int JournalEntriesCount { get; set; }
        public int CurrentStreakCount { get; set; }

        // Collections
        public IEnumerable<WorkoutCardViewModel> RecentWorkouts { get; set; }
            = new HashSet<WorkoutCardViewModel>();
        public IEnumerable<PostCardViewModel> RecentPosts { get; set; }
            = new HashSet<PostCardViewModel>();

        public IEnumerable<Community.PostCardViewModel>? ConvertedRecentPosts { get; set; }
    }
}
