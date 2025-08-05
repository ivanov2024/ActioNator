using ActioNator.Services.Interfaces.UserDashboard;
using ActioNator.ViewModels.Dashboard;
using ActioNator.ViewModels.Workouts;
using Microsoft.EntityFrameworkCore;
using ActioNator.ViewModels.Posts;
using ActioNator.Data.Models;
using System.Globalization;
using ActioNator.Data;

namespace ActioNator.Services.Implementations.UserDashboard
{
    public class UserDashboardService : IUserDashboardService
    {
        private readonly ActioNatorDbContext _dbContext;

        public UserDashboardService(ActioNatorDbContext dbContext)
            => _dbContext = dbContext 
            ?? throw new ArgumentNullException(nameof(dbContext));
        

        public async Task<DashboardViewModel> GetDashboardDataAsync(Guid userId, ApplicationUser user)
        {
            DashboardViewModel dashboardViewModel 
                = new ()
            {
                UserName 
                    = user.UserName!,
                ActiveGoalsCount 
                    = await GetActiveGoalsCountAsync(userId),
                JournalEntriesCount 
                    = await GetJournalEntriesCountAsync(userId),
                CurrentStreakCount 
                    = CalculateUserStreak(user),
                RecentWorkouts 
                    = await GetRecentWorkoutsAsync(userId),
                RecentPosts 
                    = await GetRecentPostsAsync(userId)
            };

            return dashboardViewModel;
        }

        private async Task<int> GetActiveGoalsCountAsync(Guid userId)
            => await _dbContext
            .Goals
            .Where(g => g.ApplicationUserId == userId 
                && !g.IsCompleted)
            .CountAsync();
        

        private async Task<int> GetJournalEntriesCountAsync(Guid userId)
            => await _dbContext
            .JournalEntries
            .Where(j => j.UserId == userId)
            .CountAsync();
        

        private int CalculateUserStreak(ApplicationUser user)
        {
            if (user.LastLoginAt == null)
                return 0;
            
            // Get the current date at midnight for comparison
            DateTime today = DateTime.Today;
            DateTime? lastLogin = user.LastLoginAt.Value.Date;

            // If the user hasn't logged in today or yesterday, streak is broken
            if (lastLogin < today.AddDays(-1))
                 return 0;
            
            // Count consecutive days logged in by checking login history
            int streakCount = 0;
            DateTime currentDate = today;

            // Only checking up to 100 days back to avoid excessive processing :)
            for (int i = 0; i < 100; i++)
            {
                // Check if there's a login record for this date
                bool hasLoginForDate 
                    = _dbContext
                    .UserLoginHistories
                    .Any(uhl => uhl.UserId == user.Id 
                    && uhl.LoginDate.Date == currentDate.AddDays(-i));

                if (hasLoginForDate)
                {
                    streakCount++;
                }
                else
                {
                    // Break the streak if a day was missed
                    break;
                }
            }

            return streakCount;
        }

        private async Task<IEnumerable<WorkoutCardViewModel>> GetRecentWorkoutsAsync(Guid userId)
            =>  await _dbContext
                .Workouts
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CompletedAt)
                .Take(3)
                .Select(w => new WorkoutCardViewModel
                    {
                        Id = w.Id,
                        Title = w.Title,
                        Duration = w.Duration,
                        CompletedAt = w.CompletedAt
                    })
                .ToListAsync();


        private async Task<IEnumerable<PostCardViewModel>> GetRecentPostsAsync(Guid userId)
            => await _dbContext
                .Posts
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .Include(p => p.ApplicationUser)
                .Include(p => p.PostImages)
                .Include(p => p.Comments).ThenInclude(c => c.Author)
                .AsSplitQuery()
                .Select(p => new PostCardViewModel
                {
                    Id = p.Id,
                    Content = p.Content!,
                    AuthorId = p.UserId,
                    AuthorName = p.ApplicationUser.UserName!,
                    ProfilePictureUrl = p.ApplicationUser.ProfilePictureUrl,
                    CreatedAt = p.CreatedAt,
                    LikesCount = p.LikesCount,
                    CommentsCount = p.Comments.Count,
                    SharesCount = p.SharesCount,
                    TimeAgo = GetTimeAgo(p.CreatedAt),
                    IsAuthor = p.UserId == userId,
                    IsPublic = p.IsPublic,
                    IsDeleted = p.IsDeleted,
                    ImageUrl = p.ImageUrl,
                    Images = p.PostImages
                        .Select(pi => new PostImagesViewModel
                    {
                        Id = pi.Id,
                        ImageUrl = pi.ImageUrl,
                        PostId = pi.PostId,
                    }),
                    Comments = p.Comments
                        .Select(c => new PostCommentsViewModel
                        {
                            Id = c.Id,
                            Content = c.Content,
                            AuthorName = c.Author.UserName!,
                            AuthorId = c.AuthorId,
                            ProfilePictureUrl = c.Author.ProfilePictureUrl,
                            CreatedAt = c.CreatedAt,
                            LikesCount = c.LikesCount,
                            TimeAgo = GetTimeAgo(c.CreatedAt),
                            IsDeleted = c.IsDeleted,
                            IsAuthor = c.AuthorId == userId,
                        })
                })
                .ToListAsync();
        
        #region Helper Method
        private static string GetTimeAgo(DateTime dateTime)
        {
            TimeSpan timeSpan 
                = DateTime.UtcNow - dateTime;

            return timeSpan.TotalMinutes switch
            {
                < 1 => "just now",
                < 60 => $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes < 2 ? "" : "s")} ago",
                < 1440 => $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours < 2 ? "" : "s")} ago",
                < 10080 => $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays < 2 ? "" : "s")} ago",
                _ => dateTime.ToString("D", CultureInfo.CurrentCulture),
            };
        }
        #endregion
    }
}
