using ActioNator.ViewModels.Workout;
using ActioNator.ViewModels.Workouts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.WorkoutService
{
    public interface IWorkoutService
    {
        /// <summary>
        /// Gets all workouts for a specific user
        /// </summary>
        /// <param name="userId">The user's ID</param>
        /// <returns>A collection of workout view models</returns>
        Task<IEnumerable<WorkoutCardViewModel>> GetAllWorkoutsAsync(Guid? userId);

        /// <summary>
        /// Gets a specific workout by ID
        /// </summary>
        /// <param name="workoutId">The workout ID</param>
        /// <param name="userId">The user's ID (for authorization)</param>
        /// <returns>The workout view model if found, null otherwise</returns>
        Task<WorkoutCardViewModel?> GetWorkoutByIdAsync(Guid? workoutId, Guid? userId);

        /// <summary>
        /// Creates a new workout
        /// </summary>
        /// <param name="workout">The workout to create</param>
        /// <returns>The created workout with ID</returns>
        Task<WorkoutCardViewModel> CreateWorkoutAsync(WorkoutCardViewModel workout, Guid? userId);

        /// <summary>
        /// Updates an existing workout
        /// </summary>
        /// <param name="workout">The workout with updated information</param>
        /// <param name="userId">The user's ID (for authorization)</param>
        /// <returns>The updated workout</returns>
        Task<WorkoutCardViewModel> UpdateWorkoutAsync(WorkoutCardViewModel workout, Guid? userId);

        /// <summary>
        /// Deletes a workout
        /// </summary>
        /// <param name="workoutId">The workout ID to delete</param>
        /// <param name="userId">The user's ID (for authorization)</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteWorkoutAsync(Guid? workoutId, Guid? userId);

        /// <summary>
        /// Adds an exercise to a workout
        /// </summary>
        /// <param name="exercise">The exercise to add</param>
        /// <param name="userId">The user's ID (for authorization)</param>
        /// <returns>The added exercise with ID</returns>
        Task<ExerciseViewModel> AddExerciseAsync(ExerciseViewModel exercise, Guid? userId);

        /// <summary>
        /// Updates an existing exercise
        /// </summary>
        /// <param name="exercise">The exercise with updated information</param>
        /// <param name="userId">The user's ID (for authorization)</param>
        /// <returns>The updated exercise</returns>
        Task<ExerciseViewModel> UpdateExerciseAsync(ExerciseViewModel exercise, Guid? userId);

        /// <summary>
        /// Deletes an exercise
        /// </summary>
        /// <param name="exerciseId">The exercise ID to delete</param>
        /// <param name="userId">The user's ID (for authorization)</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteExerciseAsync(Guid? exerciseId, Guid? userId);

        /// <summary>
        /// Gets all available exercise templates
        /// </summary>
        /// <returns>A collection of exercise templates</returns>
        Task<IEnumerable<ExerciseTemplateViewModel>> GetExerciseTemplatesAsync();

        /// <summary>
        /// Verifies if a workout belongs to the user
        /// </summary>
        /// <param name="workoutId">The workout ID to check</param>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> VerifyWorkoutOwnershipAsync
        (
            Guid? workoutId,
            Guid? userId,
            CancellationToken cancellationToken = default
        );

        Task<bool> VerifyExerciseOwnershipAsync
        (
            Guid? exerciseId,
            Guid? userId,
            CancellationToken cancellationToken = default
        );
    }
}
