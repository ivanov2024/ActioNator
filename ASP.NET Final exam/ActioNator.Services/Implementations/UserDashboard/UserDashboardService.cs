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
                    = user.FirstName + " " + user.LastName,
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
            .AsNoTracking()
            .Where(j => j.UserId == userId)
            .CountAsync();

        private async Task<IEnumerable<WorkoutCardViewModel>> GetRecentWorkoutsAsync(Guid userId)
            =>  await 
            _dbContext
            .Workouts
            .AsNoTracking()
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
        {
            List<Post>? posts 
                = await 
                _dbContext
                .Posts
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .Include(p => p.ApplicationUser)
                .Include(p => p.PostImages)
                .Include(p => p.Comments)
                .ThenInclude(c => c.Author)
                .AsSplitQuery()
                .ToListAsync();

            // Map to view models in memory to safely use GetTimeAgo()
            IEnumerable<PostCardViewModel>? result 
                = posts
                .Select(p => new PostCardViewModel
                {
                    Id = p.Id,
                    Content = p.Content!,
                    AuthorId = p.UserId,
                    AuthorName 
                        = p.ApplicationUser?.UserName!,
                    ProfilePictureUrl 
                        = p.ApplicationUser?.ProfilePictureUrl!,
                    CreatedAt = p.CreatedAt,
                    LikesCount = p.LikesCount,
                    CommentsCount 
                        = p.Comments?
                        .Count(c => !c.IsDeleted) ?? 0,
                    SharesCount = p.SharesCount,
                    TimeAgo = GetTimeAgo(p.CreatedAt),
                    IsAuthor = p.UserId == userId,
                    IsPublic = p.IsPublic,
                    IsDeleted = p.IsDeleted,
                    ImageUrl = p.ImageUrl,
                    Images = p.PostImages?
                        .Select(pi => new PostImagesViewModel
                        {
                            Id = pi.Id,
                            ImageUrl = pi.ImageUrl,
                            PostId = pi.PostId ?? Guid.Empty,
                        }).ToList() ?? [],
                    Comments 
                        = p.Comments?
                        .Where(c => !c.IsDeleted)
                        .Select(c => new PostCommentsViewModel
                        {
                            Id = c.Id,
                            Content = c.Content,
                            AuthorName = c.Author?.UserName!,
                            AuthorId = c.AuthorId,
                            ProfilePictureUrl = c.Author?.ProfilePictureUrl!,
                            CreatedAt = c.CreatedAt,
                            LikesCount = c.LikesCount,
                            TimeAgo = GetTimeAgo(c.CreatedAt),
                            IsDeleted = c.IsDeleted,
                            IsAuthor = c.AuthorId == userId,
                        }).ToList() ?? []
                });

            return result;
        }

        #region Helper Method

        private int CalculateUserStreak(ApplicationUser user)
        {
            if (user.LastLoginAt == null)
                return 0;

            DateTime today = DateTime.Today;

            // Fetch all login dates for this user from last 100 days (or since earliest allowed date)
            DateTime cutoffDate = today
                .AddDays(-99); // including today counts as day 0

            List<DateTime>? loginDates 
                = _dbContext
                .UserLoginHistories
                .Where(uhl => uhl.UserId == user.Id 
                    && uhl.LoginDate.Date >= cutoffDate)
                .Select(uhl => uhl.LoginDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToList();

            if (loginDates.Count == 0 
                || loginDates.First() < today.AddDays(-1))
                return 0; // Streak broken if last login is before yesterday

            int streakCount = 0;
            DateTime streakDate = today;

            foreach (DateTime loginDate in loginDates)
            {
                if (loginDate == streakDate 
                    || loginDate == streakDate.AddDays(-1))
                {
                    streakCount++;
                    streakDate = streakDate.AddDays(-1);
                }
                else if (loginDate < streakDate.AddDays(-1))
                {
                    // Missed a day - streak ends
                    break;
                }
                // If loginDate > streakDate, just continue (could be multiple logins same day)
            }

            return streakCount;
        }

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
