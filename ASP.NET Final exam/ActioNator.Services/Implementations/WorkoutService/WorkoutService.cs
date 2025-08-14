using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.WorkoutService;
using ActioNator.ViewModels.Workout;
using ActioNator.ViewModels.Workouts;
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActioNator.Services.Implementations.WorkoutService
{
    public class WorkoutService : IWorkoutService
    {
        private readonly ActioNatorDbContext _dbContext;
        private readonly ILogger<WorkoutService> _logger;

        public WorkoutService(ActioNatorDbContext dbContext, ILogger<WorkoutService> logger)
        {
            _dbContext = dbContext
                ?? throw new ArgumentNullException(nameof(dbContext), "Database context cannot be null");

            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null");
        }

        public async Task<(IEnumerable<WorkoutCardViewModel> Workouts, int TotalCount)> GetWorkoutsPageAsync(Guid? userId, int page, int pageSize)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogError("Attempted to retrieve workouts with an empty user ID.");
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            }

            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 3;

            var baseQuery = _dbContext
                .Workouts
                .AsNoTracking()
                .Where(w => w.UserId == userId);

            int totalCount = await baseQuery.CountAsync();

            List<Workout>? workouts = await baseQuery
                .Include(w => w.Exercises)
                .ThenInclude(e => e.ExerciseTemplate)
                .OrderByDescending(w => w.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsSplitQuery()
                .ToListAsync();

            var mapped = workouts.Select(w => new WorkoutCardViewModel
            {
                Id = w.Id,
                Title = w.Title ?? string.Empty,
                Notes = w.Notes ?? string.Empty,
                Date = w.Date,
                Duration = CalculateWorkoutDuration(w.Exercises),
                CompletedAt = w.CompletedAt,
                IsCompleted = w.CompletedAt != null,
                Exercises = (w.Exercises ?? Enumerable.Empty<Exercise>())
                    .Select(e => new ExerciseViewModel
                    {
                        Id = e.Id,
                        WorkoutId = e.WorkoutId,
                        Name = e.ExerciseTemplate?.Name ?? string.Empty,
                        Sets = e.Sets,
                        Reps = e.Reps,
                        Weight = e.Weight,
                        Notes = e.Notes ?? string.Empty,
                        Duration = (int)e.Duration.TotalMinutes,
                        ExerciseTemplateId = e.ExerciseTemplateId,
                        TargetedMuscle = e.ExerciseTemplate?.TargetedMuscle ?? string.Empty
                    }).ToList()
            });

            return (mapped, totalCount);
        }

        public async Task<IEnumerable<WorkoutCardViewModel>> GetAllWorkoutsAsync(Guid? userId)
        {
            if (userId == Guid.Empty)
            {
                _logger.LogError("Attempted to retrieve workouts with an empty user ID.");

                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            }

            List<Workout>? workouts
                = await
                _dbContext
                .Workouts
                .AsNoTracking()
                .Where(w => w.UserId == userId)
                .Include(w => w.Exercises)
                .ThenInclude(e => e.ExerciseTemplate)
                .OrderByDescending(w => w.Date)
                .AsSplitQuery()
                .ToListAsync();

            return workouts.Select(w => new WorkoutCardViewModel
            {
                Id = w.Id,
                Title = w.Title ?? string.Empty,
                Notes = w.Notes ?? string.Empty,
                Date = w.Date,
                Duration = CalculateWorkoutDuration(w.Exercises),
                CompletedAt = w.CompletedAt,
                IsCompleted = w.CompletedAt != null,
                Exercises = (w.Exercises ?? Enumerable.Empty<Exercise>())
                .Select(e => new ExerciseViewModel
                {
                    Id = e.Id,
                    WorkoutId = e.WorkoutId,
                    Name = e.ExerciseTemplate?.Name ?? string.Empty,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    Weight = e.Weight,
                    Notes = e.Notes ?? string.Empty,
                    Duration = (int)e.Duration.TotalMinutes,
                    ExerciseTemplateId = e.ExerciseTemplateId,
                    TargetedMuscle = e.ExerciseTemplate?.TargetedMuscle ?? string.Empty
                }).ToList()
            });
        }

        public async Task<WorkoutCardViewModel?> GetWorkoutByIdAsync(Guid? workoutId, Guid? userId)
        {
            if (userId == Guid.Empty)
            {
                _logger
                    .LogError("Attempted to retrieve a workout with an empty user ID.");

                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            }

            Workout? workout
                = await
                _dbContext
                .Workouts
                .AsNoTracking()
                .Include(w => w.Exercises)
                .ThenInclude(e => e.ExerciseTemplate)
                .FirstOrDefaultAsync(w => w.Id == workoutId
                    && w.UserId == userId);

            if (workout == null)
                return null;

            return new WorkoutCardViewModel
            {
                Id = workout.Id,
                Title = workout.Title ?? string.Empty,
                Notes = workout.Notes ?? string.Empty,
                Date = workout.Date,
                Duration = workout.Duration,
                CompletedAt = workout.CompletedAt,
                IsCompleted = workout.CompletedAt != null,
                Exercises = (workout.Exercises ?? Enumerable.Empty<Exercise>())
                .Select(e => new ExerciseViewModel
                {
                    Id = e.Id,
                    WorkoutId = e.WorkoutId,
                    Name = e.ExerciseTemplate?.Name!,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    Weight = e.Weight,
                    Notes = e.Notes ?? string.Empty,
                    Duration = (int)e.Duration.TotalMinutes,
                    ExerciseTemplateId = e.ExerciseTemplateId,
                    TargetedMuscle = e.ExerciseTemplate?.TargetedMuscle ?? string.Empty
                }).ToList()
            };
        }

        public async Task<WorkoutCardViewModel> CreateWorkoutAsync(WorkoutCardViewModel workout, Guid? userId)
        {
            if (userId == null)
            {
                _logger.LogError("Attempted to create a workout without a valid user ID.");

                throw new ArgumentNullException(nameof(userId), "User ID cannot be null when creating a workout");
            }

            if (workout == null)
            {
                _logger.LogError("Attempted to create a workout with a null workout model.");

                throw new ArgumentNullException(nameof(workout), "Workout cannot be null");
            }

            if (string.IsNullOrWhiteSpace(workout.Title))
            {  
                _logger.LogError("Attempted to create a workout with an empty title.");

                throw new ArgumentException("Workout title cannot be empty.", nameof(workout.Title));
            }

            Workout newWorkout = new()
            {
                Id = Guid.NewGuid(),
                Title = workout.Title.Trim(),
                // The date must come directly from the user's input; no server-side fallback/defaults.
                // Controller-level model validation guarantees Date has a valid value here.
                Date = workout.Date 
                ?? throw new ArgumentException("Date is required and must be valid.", nameof(workout.Date)),
                Notes = workout.Notes?.Trim(),
                Duration = TimeSpan.Zero,
                UserId = userId.Value
            };

            await
                _dbContext
                .Workouts
                .AddAsync(newWorkout);

            await
                _dbContext
                .SaveChangesAsync();

            // Map entity to ViewModel to ensure consistent returned data
            return new WorkoutCardViewModel
            {
                Id = newWorkout.Id,
                Title = newWorkout.Title,
                Date = newWorkout.Date,
                Notes = newWorkout.Notes,
                Duration = newWorkout.Duration,
                CompletedAt = newWorkout.CompletedAt,
                IsCompleted = newWorkout.CompletedAt != null
            };
        }

        public async Task<WorkoutCardViewModel> UpdateWorkoutAsync(WorkoutCardViewModel workout, Guid? userId)
        {
            if (userId == null || workout.Id == Guid.Empty)
            {
                _logger.LogError("Attempted to update a workout without a valid user ID or workout ID.");
                throw new ArgumentException("Invalid parameters.");
            }

            // Find the workout
            Workout? existingWorkout
                = await
                _dbContext
                .Workouts
                .Include(w => w.Exercises)
                .FirstOrDefaultAsync(w => w.Id == workout.Id
                    && w.UserId == userId);


            if (existingWorkout == null)
            {
                _logger
                    .LogWarning($"Workout with ID {workout.Id} not found for user {userId}.");

                throw new InvalidOperationException("Workout not found or you don't have permission to update it.");
            }

            // Update properties
            existingWorkout.Title = workout.Title;
            existingWorkout.Notes = workout.Notes;
            // Update date (validated at controller level)
            if (workout.Date.HasValue)
            {
                existingWorkout.Date = workout.Date.Value;
            }

            // Toggle completion state based on IsCompleted flag
            if (workout.IsCompleted)
            {
                if (!existingWorkout.CompletedAt.HasValue)
                {
                    existingWorkout.CompletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // If user unchecks completion, clear the timestamp
                if (existingWorkout.CompletedAt.HasValue)
                {
                    existingWorkout.CompletedAt = null;
                }
            }

            // Recalculate total duration from non-deleted exercises using ticks for precision
            existingWorkout.Duration = CalculateWorkoutDuration(
                existingWorkout.Exercises?.Where(e => !e.IsDeleted)
            );

            await
                _dbContext
                .SaveChangesAsync();

            // Map updated entity back to ViewModel
            return new WorkoutCardViewModel
            {
                Id = existingWorkout.Id,
                Title = existingWorkout.Title,
                Notes = existingWorkout.Notes,
                Date = existingWorkout.Date,
                Duration = existingWorkout.Duration,
                CompletedAt = existingWorkout.CompletedAt,
                IsCompleted = existingWorkout.CompletedAt != null
            };
        }

        public async Task<bool> DeleteWorkoutAsync(Guid? workoutId, Guid? userId)
        {
            if (workoutId == Guid.Empty)
            {
                _logger.LogError("Attempted to delete a workout with an empty ID.");

                throw new ArgumentException("Workout ID cannot be empty.", nameof(workoutId));
            }

            if (!userId.HasValue || userId == Guid.Empty)
            {
                _logger.LogError("Attempted to delete a workout without a valid user ID.");

                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            Workout? workout
                = await
                _dbContext
                .Workouts
                .SingleOrDefaultAsync(w => w.Id == workoutId
                    && w.UserId == userId);

            if (workout == null)
            {
                _logger.LogCritical($"Workout with ID {workoutId} not found or does not belong to user {userId}.");
                return false;
            }

            workout
                .IsDeleted = true;

            await
                _dbContext
                .SaveChangesAsync();

            return true;
        }

        public async Task<ExerciseViewModel> AddExerciseAsync(ExerciseViewModel exercise, Guid? userId)
        {
            if (!userId.HasValue || userId == Guid.Empty)
            {
                _logger.LogError("Attempted to add an exercise without a valid user ID.");

                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (exercise == null)
            {
                _logger
                    .LogError("Attempted to add a null exercise.");
                throw new ArgumentNullException(nameof(exercise));
            }

            // Verify workout ownership
            Workout? workout
                = await
                _dbContext
                .Workouts
                .FirstOrDefaultAsync(w => w.Id == exercise.WorkoutId
                    && w.UserId == userId);

            if (workout == null)
            {
                _logger
                    .LogCritical($"Workout with ID {exercise.WorkoutId} not found for user {userId}.");

                throw new InvalidOperationException($"Workout with ID {exercise.WorkoutId} not found or you don't have permission to modify it.");
            }

            // Create new exercise
            Exercise newExercise = new()
            {
                Id = Guid.NewGuid(),
                WorkoutId = exercise.WorkoutId,
                ExerciseTemplateId = exercise.ExerciseTemplateId,
                Sets = exercise.Sets,
                Reps = exercise.Reps,
                Weight = exercise.Weight,
                Notes = exercise.Notes,
                Duration = TimeSpan.FromMinutes(exercise.Duration)
            };

            await
                _dbContext
                .Exercises
                .AddAsync(newExercise);

            await
                _dbContext
                .SaveChangesAsync();

            // Recalculate workout duration by materializing durations then summing client-side to avoid EF translation issues
            var durationsAdd = await _dbContext
                .Exercises
                .Where(e => e.WorkoutId == workout.Id && !e.IsDeleted)
                .Select(e => e.Duration)
                .ToListAsync();
            workout.Duration = durationsAdd.Count > 0
                ? TimeSpan.FromTicks(durationsAdd.Sum(d => d.Ticks))
                : TimeSpan.Zero;

            await
                _dbContext
                .SaveChangesAsync();

            // Load related template info for return
            ExerciseTemplate? template
                = await
                _dbContext
                .ExerciseTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(xt => xt.Id == newExercise.ExerciseTemplateId);

            return new ExerciseViewModel
            {
                Id = newExercise.Id,
                WorkoutId = newExercise.WorkoutId,
                Name = template!.Name,
                Sets = newExercise.Sets,
                Reps = newExercise.Reps,
                Weight = newExercise.Weight,
                Notes = newExercise.Notes,
                Duration = (int)newExercise.Duration.TotalMinutes,
                ExerciseTemplateId = newExercise.ExerciseTemplateId,
                TargetedMuscle = template?.TargetedMuscle ?? string.Empty
            };
        }

        public async Task<ExerciseViewModel> UpdateExerciseAsync(ExerciseViewModel exercise, Guid? userId)
        {
            if (!userId.HasValue || userId == Guid.Empty)
            {
                _logger.LogError("Attempted to update an exercise without a valid user ID.");

                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (exercise == null)
            {
                _logger.LogCritical("Attempted to update a null exercise.");

                throw new ArgumentNullException(nameof(exercise));
            }

            // Verify workout ownership
            Workout? workout
                = await
                _dbContext
                .Workouts
                .FirstOrDefaultAsync(w => w.Id == exercise.WorkoutId
                && w.UserId == userId);

            if (workout == null)
            {
                _logger.LogCritical($"Workout with ID {exercise.WorkoutId} not found for user {userId}.");

                throw new InvalidOperationException($"Workout with ID {exercise.WorkoutId} not found or you don't have permission to modify it.");
            }

            // Find existing exercise
            Exercise? existingExercise
                = await
                _dbContext
                .Exercises
                .FirstOrDefaultAsync(e => e.Id == exercise.Id
                    && e.WorkoutId == exercise.WorkoutId);


                if (existingExercise == null)
                {
                    _logger
                        .LogCritical($"Exercise with ID {exercise.Id} not found in workout {exercise.WorkoutId} for user {userId}.");

                    throw new InvalidOperationException($"Exercise with ID {exercise.Id} not found.");
                }

            // Update properties
            existingExercise.ExerciseTemplateId = exercise.ExerciseTemplateId;
            existingExercise.Sets = exercise.Sets;
            existingExercise.Reps = exercise.Reps;
            existingExercise.Weight = exercise.Weight;
            existingExercise.Notes = exercise.Notes;
            existingExercise.Duration = TimeSpan.FromMinutes(exercise.Duration);

            // Persist the updated exercise first so the recalculation sees the new duration value
            await
                _dbContext
                .SaveChangesAsync();

            // Recalculate workout duration by materializing durations then summing client-side to avoid EF translation issues
            var durationsUpdate = await _dbContext
                .Exercises
                .Where(e => e.WorkoutId == workout.Id && !e.IsDeleted)
                .Select(e => e.Duration)
                .ToListAsync();
            workout.Duration = durationsUpdate.Count > 0
                ? TimeSpan.FromTicks(durationsUpdate.Sum(d => d.Ticks))
                : TimeSpan.Zero;

            await 
                _dbContext
                .SaveChangesAsync();

            // Return updated view model with template info
            var result 
                = await 
                _dbContext
                .Exercises
                .Where(e => e.Id == existingExercise.Id)
                .Select(e => new ExerciseViewModel
                {
                    Id = e.Id,
                    WorkoutId = e.WorkoutId,
                    Name = e.ExerciseTemplate.Name,
                    Sets = e.Sets,
                    Reps = e.Reps,
                    Weight = e.Weight,
                    Notes = e.Notes,
                    Duration = (int)e.Duration.TotalMinutes,
                    ExerciseTemplateId = e.ExerciseTemplateId,
                    TargetedMuscle = e.ExerciseTemplate.TargetedMuscle
                })
                .FirstAsync();

            return result;
        }

        public async Task<bool> DeleteExerciseAsync(Guid? exerciseId, Guid? userId)
        {
            if (!userId.HasValue || userId == Guid.Empty)
            {
                _logger
                    .LogError("Attempted to delete an exercise without a valid user ID.");

                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            // Find exercise & ensure ownership
            Exercise? exercise 
                = await 
                _dbContext
                .Exercises
                .Include(e => e.Workout)
                .FirstOrDefaultAsync(e => e.Id == exerciseId 
                && e.Workout.UserId == userId);

            if (exercise == null)
            {
                _logger
                    .LogCritical($"Exercise with ID {exerciseId} not found or does not belong to user {userId}.");

                return false;
            }

            // Soft delete
            exercise.IsDeleted = true;

            // Recalculate workout duration by materializing durations then summing client-side to avoid EF translation issues
            var durationsDelete = await _dbContext
                .Exercises
                .Where(e => e.WorkoutId == exercise.WorkoutId 
                    && e.Id != exerciseId
                    && !e.IsDeleted)
                .Select(e => e.Duration)
                .ToListAsync();
            exercise.Workout.Duration = durationsDelete.Count > 0
                ? TimeSpan.FromTicks(durationsDelete.Sum(d => d.Ticks))
                : TimeSpan.Zero;

            await 
                _dbContext
                .SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<ExerciseTemplateViewModel>> GetExerciseTemplatesAsync()
        {
            try
            {
                return 
                    await 
                    _dbContext
                    .ExerciseTemplates
                    .AsNoTracking()
                    .Select(t => new ExerciseTemplateViewModel
                    {
                        Id = t.Id,
                        Name = t.Name ?? string.Empty,
                        ImageUrl = t.ImageUrl ?? string.Empty,
                        TargetedMuscle = t.TargetedMuscle ?? string.Empty
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error retrieving exercise templates", ex);
            }
        }

        public async Task<bool> VerifyWorkoutOwnershipAsync
        (
            Guid? workoutId,
            Guid? userId,
            CancellationToken cancellationToken = default
        )
        {
            if (userId is null || userId == Guid.Empty || workoutId == Guid.Empty)
            {
                _logger.LogCritical("Attempted to verify workout ownership with invalid parameters.");

                throw new ArgumentException("Invalid parameters for workout ownership verification.");
            }

            return 
                await 
                _dbContext
                .Workouts
                .AnyAsync(
                    w => w.Id == workoutId && w.UserId == userId.Value,
                    cancellationToken
                );
        }

        public async Task<bool> VerifyExerciseOwnershipAsync
        (
            Guid? exerciseId,
            Guid? userId,
            CancellationToken cancellationToken = default
        )
        {
            if (userId is null || userId == Guid.Empty || exerciseId == Guid.Empty)
            {
                _logger.LogCritical("Attempted to verify exercise ownership with invalid parameters.");

                throw new ArgumentException("Invalid parameters for exercise ownership verification.");
            }

            return
                await
                _dbContext
                .Exercises
                .Include(e => e.Workout)
                .AnyAsync(
                    e => e.Id == exerciseId 
                    && e.Workout.UserId == userId.Value,
                    cancellationToken
                );
        }

        /// <summary>
        /// Calculates the total duration of a workout based on its non-deleted exercises.
        /// </summary>
        /// <param name="exercises">Collection of exercises</param>
        /// <returns>Total duration as TimeSpan</returns>
        private static TimeSpan CalculateWorkoutDuration(IEnumerable<Exercise>? exercises)
        {
            if (exercises is null)
            {
                return TimeSpan.Zero;
            }

            long totalTicks = exercises
                .Sum(e => e.Duration.Ticks);

            return TimeSpan.FromTicks(totalTicks);
        }
    }
}
