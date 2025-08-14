using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.GoalService;
using ActioNator.ViewModels.Goal;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ActioNator.Services.Implementations.GoalService
{
    /// <summary>
    /// Implementation of the IGoalService interface for managing user goals
    /// </summary>
    public class GoalService : IGoalService
    {
        private readonly ActioNatorDbContext _dbContext;
        private readonly ILogger<GoalService> _logger;
        private readonly IClock _clock;

        /// <summary>
        /// Constructor for GoalService
        /// </summary>
        /// <param name="dbContext">DbContext for goal data access</param>
        /// <param name="logger">Logger for service operations</param>
        /// <param name="clock">Clock for time operations (default: SystemClock)</param>
        public GoalService(ActioNatorDbContext dbContext, ILogger<GoalService> logger, IClock? clock = null)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clock = clock ?? SystemClock.Instance;
        }

        /// <summary>
        /// Retrieves all goals for a specific user with optional filtering
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="filter">Optional filter: "all", "active", "completed", or "overdue"</param>
        /// <returns>A list of goal view models</returns>
        public async Task<List<GoalViewModel>> GetUserGoalsAsync(Guid? userId, string filter = "all", CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                }

                IQueryable<Goal>? query
                    = _dbContext
                    .Goals
                    .Where(g => g.ApplicationUserId == userId)
                    .AsNoTracking();

                // Apply filtering
                switch (filter.ToLower())
                {
                    case "active":
                        query 
                            = query.Where(g => !g.IsCompleted 
                                && g.DueDate >= _clock.UtcNow);
                        break;
                    case "completed":
                        query 
                            = query.Where(g => g.IsCompleted);
                        break;
                    case "overdue":
                        query 
                            = query.Where(g => !g.IsCompleted 
                            && g.DueDate < _clock.UtcNow);
                        break;
                    // "all" or any other value returns all goals
                }

                var goals 
                    = await query
                    .OrderBy(g => g.IsCompleted)
                    .ThenBy(g => g.DueDate)
                    .Select(g => new GoalViewModel
                    {
                        Id = g.Id,
                        Title = g.Title,
                        Description = g.Description,
                        DueDate = g.DueDate,
                        Completed = g.IsCompleted
                    })
                    .ToListAsync(cancellationToken);

                return goals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals for user {UserId} with filter {Filter}", userId, filter);
                throw;
            }
        }

        /// <summary>
        /// Creates a new goal for a user
        /// </summary>
        /// <param name="model">The goal view model containing goal details</param>
        /// <param name="userId">The ID of the user creating the goal</param>
        /// <returns>The created goal view model with assigned ID</returns>
        public async Task<GoalViewModel> CreateGoalAsync(GoalViewModel model, Guid? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(model);

                if (userId == Guid.Empty)
                {
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                }

                Goal goal = new ()
                {
                    Title = model.Title,
                    Description = model.Description,
                    DueDate = DateTime.SpecifyKind(model.DueDate, DateTimeKind.Utc),
                    IsCompleted = model.Completed,
                    ApplicationUserId = userId!.Value,
                    CreatedAt = _clock.UtcNow
                };

                await _dbContext
                    .AddAsync(goal, cancellationToken);

                await _dbContext
                    .SaveChangesAsync(cancellationToken);

                // Update the model with the generated ID
                model.Id = goal.Id;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing goal
        /// </summary>
        /// <param name="model">The goal view model with updated information</param>
        /// <param name="userId">The ID of the user performing the update</param>
        /// <returns>The updated goal view model</returns>
        public async Task<GoalViewModel> UpdateGoalAsync(GoalViewModel model, Guid? userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (model == null)
                {
                    throw new ArgumentNullException(nameof(model));
                }

                if (model.Id == Guid.Empty)
                {
                    throw new ArgumentException("Goal ID cannot be null or empty", nameof(model.Id));
                }
                
                if (userId == null || userId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                var goal = await _dbContext
                    .Goals
                    .SingleOrDefaultAsync(g => g.Id == model.Id, cancellationToken);

                if (goal == null)
                {
                    throw new InvalidOperationException($"Goal with ID {model.Id} not found");
                }

                if (goal.ApplicationUserId != userId)
                {
                    throw new UnauthorizedAccessException("You do not have permission to update this goal");
                }

                // Update properties
                goal.Title = model.Title;
                goal.Description = model.Description;
                goal.DueDate = DateTime.SpecifyKind(model.DueDate, DateTimeKind.Utc);
                goal.IsCompleted = model.Completed;

                if (model.Completed)
                {
                    goal.CompletedAt = _clock.UtcNow;
                }
                else
                {
                    goal.CompletedAt = null;
                }

                _dbContext.Update(goal);

                await _dbContext
                    .SaveChangesAsync(cancellationToken);

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal with ID {GoalId}", model?.Id);
                throw;
            }
        }

        /// <summary>
        /// Deletes a goal by its ID
        /// </summary>
        /// <param name="goalId">The ID of the goal to delete</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (goalId == Guid.Empty)
                {
                    throw new ArgumentException("Goal ID cannot be empty", nameof(goalId));
                }

                var goal = await _dbContext
                    .Goals
                    .SingleOrDefaultAsync(g => g.Id == goalId, cancellationToken);

                if (goal == null)
                {
                    throw new InvalidOperationException($"Goal with ID {goalId} not found");
                }

                 _dbContext
                    .Remove(goal);

                await _dbContext
                    .SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal with ID {GoalId}", goalId);
                throw;
            }
        }

        /// <summary>
        /// Toggles the completion status of a goal
        /// </summary>
        /// <param name="goalId">The ID of the goal to toggle</param>
        /// <returns>The updated goal view model</returns>
        public async Task<GoalViewModel> ToggleGoalCompletionAsync(Guid goalId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (goalId == Guid.Empty)
                {
                    throw new ArgumentException("Goal ID cannot be empty", nameof(goalId));
                }

                var goal = await _dbContext
                    .Goals
                    .SingleOrDefaultAsync(g => g.Id == goalId, cancellationToken);
                if (goal == null)
                {
                    throw new InvalidOperationException($"Goal with ID {goalId} not found");
                }

                // Toggle completion status
                goal.IsCompleted = !goal.IsCompleted;
                goal.CompletedAt = goal.IsCompleted ? _clock.UtcNow : null;

                // Don't use AddAsync for existing entities - it's for new entities only
                _dbContext.Update(goal);

                await _dbContext
                    .SaveChangesAsync(cancellationToken);

                return new GoalViewModel
                {
                    Id = goal.Id,
                    Title = goal.Title,
                    Description = goal.Description,
                    DueDate = goal.DueDate,
                    Completed = goal.IsCompleted
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling completion for goal with ID {GoalId}", goalId);
                throw;
            }
        }

        /// <summary>
        /// Verifies if a user is the owner of a specific goal
        /// </summary>
        /// <param name="goalId">The ID of the goal to check</param>
        /// <param name="userId">The ID of the user</param>
        /// <returns>True if the user is the owner, otherwise false</returns>
        public async Task<bool> VerifyGoalOwnershipAsync(Guid goalId, Guid? userId, CancellationToken cancellationToken = default)
        {
            if (userId == null)
                return false;

            // Check if the workout exists and belongs to the user
            return await _dbContext
                .Goals
                .AnyAsync(g => g.Id == goalId && g.ApplicationUserId == userId, cancellationToken);
        }
    }
}

// Minimal clock abstraction for UTC normalization
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new SystemClock();
    public DateTime UtcNow => DateTime.UtcNow;
}
