using ActioNator.ViewModels.Goal;
using System.Threading;

namespace ActioNator.Services.Interfaces.GoalService
{
    /// <summary>
    /// Service interface for managing user goals
    /// </summary>
    public interface IGoalService
    {
        /// <summary>
        /// Retrieves all goals for a specific user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="filter">Optional filter: "all", "active", "completed", or "overdue"</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A list of goal view models</returns>
        Task<List<GoalViewModel>> GetUserGoalsAsync(Guid? userId, string filter = "all", CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new goal for a user
        /// </summary>
        /// <param name="model">The goal view model containing goal details</param>
        /// <param name="userId">The ID of the user creating the goal</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created goal view model with assigned ID</returns>
        Task<GoalViewModel> CreateGoalAsync(GoalViewModel model, Guid? userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing goal
        /// </summary>
        /// <param name="model">The goal view model with updated information</param>
        /// <param name="userId">The ID of the user performing the update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated goal view model</returns>
        Task<GoalViewModel> UpdateGoalAsync(GoalViewModel model, Guid? userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a goal by its ID
        /// </summary>
        /// <param name="goalId">The ID of the goal to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task DeleteGoalAsync(Guid goalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Toggles the completion status of a goal
        /// </summary>
        /// <param name="goalId">The ID of the goal to toggle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated goal view model</returns>
        Task<GoalViewModel> ToggleGoalCompletionAsync(Guid goalId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies if a user is the owner of a specific goal
        /// </summary>
        /// <param name="goalId">The ID of the goal to check</param>
        /// <param name="userId">The ID of the user</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the user is the owner, otherwise false</returns>
        Task<bool> VerifyGoalOwnershipAsync(Guid goalId, Guid? userId, CancellationToken cancellationToken = default);
    }
}
