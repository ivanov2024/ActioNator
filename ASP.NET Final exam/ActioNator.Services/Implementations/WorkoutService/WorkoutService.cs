using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.Services.Interfaces.WorkoutService;
using ActioNator.ViewModels.Workout;
using ActioNator.ViewModels.Workouts;
using Microsoft.EntityFrameworkCore;

namespace ActioNator.Services.Implementations.WorkoutService
{
    public class WorkoutService : IWorkoutService
    {
        private readonly ActioNatorDbContext _dbContext;

        public WorkoutService(ActioNatorDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<WorkoutCardViewModel>> GetAllWorkoutsAsync(Guid? userId)
        {
            // Fetch workouts with includes to prevent null reference exceptions
            var workouts = await _dbContext.Workouts
                .AsNoTracking()
                .Include(w => w.Exercises.Where(e => !e.IsDeleted))
                    .ThenInclude(e => e.ExerciseTemplate)
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .OrderByDescending(w => w.Date)
                .AsSplitQuery()
                .ToListAsync();

            // Then map to view models with client-side evaluation and null guards
            return workouts.Select(w => new WorkoutCardViewModel
            {
                Id = w.Id,
                Title = w.Title ?? string.Empty,
                Notes = w.Notes ?? string.Empty,
                Duration = CalculateWorkoutDuration(w.Exercises),
                CompletedAt = w.CompletedAt,
                Exercises = (w.Exercises ?? Enumerable.Empty<Exercise>())
                    .Where(e => !e.IsDeleted)
                    .Select(e => new ExerciseViewModel
                    {
                        Id = e.Id,
                        WorkoutId = e.WorkoutId,
                        Name = e.ExerciseTemplate?.Name ?? "Unknown Exercise",
                        Sets = e.Sets,
                        Reps = e.Reps,
                        Weight = e.Weight,
                        Notes = e.Notes ?? string.Empty,
                        Duration = (int)(e.Duration.TotalMinutes),
                        ExerciseTemplateId = e.ExerciseTemplateId,
                        ImageUrl = e.ExerciseTemplate?.ImageUrl ?? string.Empty,
                        TargetedMuscle = e.ExerciseTemplate?.TargetedMuscle ?? string.Empty
                    }).ToList() ?? new List<ExerciseViewModel>()
            }).ToList();
        }

        public async Task<WorkoutCardViewModel?> GetWorkoutByIdAsync(Guid workoutId, Guid? userId)
        {
            // First fetch the workout with basic properties and include related entities
            var workout = await _dbContext
                .Workouts
                .AsNoTracking()
                .Include(w => w.Exercises.Where(e => !e.IsDeleted))
                    .ThenInclude(e => e.ExerciseTemplate)
                .Where(w => w.Id == workoutId
                    && w.UserId == userId && !w.IsDeleted)
                .FirstOrDefaultAsync();

            // Return null if workout not found
            if (workout == null)
                return null;

            // Then map to view model with client-side evaluation and null guards
            return new WorkoutCardViewModel
            {
                Id = workout.Id,
                Title = workout.Title ?? string.Empty,
                Notes = workout.Notes ?? string.Empty,
                Duration = workout.Duration,
                CompletedAt = workout.CompletedAt,
                Exercises = (workout.Exercises ?? Enumerable.Empty<Exercise>())
                    .Select(e => new ExerciseViewModel
                    {
                        Id = e.Id,
                        WorkoutId = e.WorkoutId,
                        Name = e.ExerciseTemplate?.Name ?? "Unknown Exercise",
                        Sets = e.Sets,
                        Reps = e.Reps,
                        Weight = e.Weight,
                        Notes = e.Notes ?? string.Empty,
                        Duration = (int)(e.Duration.TotalMinutes),
                        ExerciseTemplateId = e.ExerciseTemplateId,
                        ImageUrl = e.ExerciseTemplate?.ImageUrl ?? string.Empty,
                        TargetedMuscle = e.ExerciseTemplate?.TargetedMuscle ?? string.Empty
                    }).ToList() ?? new List<ExerciseViewModel>()
            };
        }

        public async Task<WorkoutCardViewModel> CreateWorkoutAsync(WorkoutCardViewModel workout, Guid? userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId), "User ID cannot be null when creating a workout");
            }

            // Create a new workout entity
            var newWorkout = new Workout
            {
                Id = Guid.NewGuid(),
                Title = workout.Title,
                Date = DateTime.UtcNow.Date, // Use current date since Date is not in WorkoutCardViewModel
                Notes = workout.Notes,
                Duration = TimeSpan.Zero, // Will be updated if exercises are added later
                UserId = userId.Value // Use the provided userId
            };

            // Add the workout to the database
            await _dbContext.Workouts.AddAsync(newWorkout);
            await _dbContext.SaveChangesAsync();

            // Return the created workout with ID
            workout.Id = newWorkout.Id;
            return workout;
        }

        public async Task<WorkoutCardViewModel> UpdateWorkoutAsync(WorkoutCardViewModel workout, Guid? userId)
        {
            // Find the workout
            var existingWorkout = await _dbContext.Workouts
                .FirstOrDefaultAsync(w => w.Id == workout.Id && w.UserId == userId && !w.IsDeleted);

            if (existingWorkout == null)
                throw new InvalidOperationException("Workout not found or you don't have permission to update it.");

            // Update the workout entity
            existingWorkout.Title = workout.Title;
            existingWorkout.Notes = workout.Notes;

            // Duration will be recalculated from exercises, not taken from the model

            await _dbContext.SaveChangesAsync();
            return workout;
        }

        public async Task<bool> DeleteWorkoutAsync(Guid workoutId, Guid? userId)
        {
            Workout? workout
                = await _dbContext
                .Workouts
                .FirstOrDefaultAsync(w => w.Id == workoutId
                && w.UserId == userId && !w.IsDeleted);

            if (workout == null)
            {
                return false;
            }

            // Soft delete
            workout.IsDeleted = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ExerciseViewModel> AddExerciseAsync(ExerciseViewModel exercise, Guid? userId)
        {
            // Verify the workout belongs to the user
            Workout? workout
                = await _dbContext
                .Workouts
                .FirstOrDefaultAsync(w => w.Id == exercise.WorkoutId
                && w.UserId == userId && !w.IsDeleted)
                ?? throw new InvalidOperationException($"Workout with ID {exercise.WorkoutId} not found or you don't have permission to modify it.");

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

            await _dbContext
                .Exercises
                .AddAsync(newExercise);
            await _dbContext.SaveChangesAsync();

            // Recalculate workout duration from all non-deleted exercises
            var exercises = await _dbContext.Exercises
                .Where(e => e.WorkoutId == workout.Id && !e.IsDeleted)
                .ToListAsync();

            workout.Duration = CalculateWorkoutDuration(exercises);


            // Reload and return the full ExerciseViewModel
            var result = await _dbContext.Exercises
                .Where(e => e.Id == newExercise.Id)
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
                    ImageUrl = e.ExerciseTemplate.ImageUrl,
                    TargetedMuscle = e.ExerciseTemplate.TargetedMuscle
                })
                .FirstOrDefaultAsync();
            return result!;
        }

        public async Task<ExerciseViewModel> UpdateExerciseAsync(ExerciseViewModel exercise, Guid? userId)
        {
            // First verify the workout belongs to the user
            Workout? workout
                = await _dbContext
                .Workouts
                .FirstOrDefaultAsync(w => w.Id == exercise.WorkoutId
                && w.UserId == userId && !w.IsDeleted)
                ?? throw new InvalidOperationException($"Workout with ID {exercise.WorkoutId} not found or you don't have permission to modify it.");

            Exercise? existingExercise
                = await _dbContext
                .Exercises
                .FirstOrDefaultAsync(e => e.Id == exercise.Id
                && e.WorkoutId == exercise.WorkoutId && !e.IsDeleted)
                ?? throw new InvalidOperationException($"Exercise with ID {exercise.Id} not found.");


            // Calculate duration difference for workout update
            TimeSpan oldDuration
                = existingExercise.Duration;
            TimeSpan newDuration
                = TimeSpan.FromMinutes(exercise.Duration);
            TimeSpan durationDifference
                = newDuration - oldDuration;

            // Update exercise properties
            existingExercise.ExerciseTemplateId = exercise.ExerciseTemplateId;
            existingExercise.Sets = exercise.Sets;
            existingExercise.Reps = exercise.Reps;
            existingExercise.Weight = exercise.Weight;
            existingExercise.Notes = exercise.Notes;
            existingExercise.Duration = newDuration;

            await _dbContext.SaveChangesAsync();

            // Recalculate workout duration from all non-deleted exercises
            var exercises = await _dbContext.Exercises
                .Where(e => e.WorkoutId == workout.Id && !e.IsDeleted)
                .ToListAsync();

            workout.Duration = CalculateWorkoutDuration(exercises);

            await _dbContext.SaveChangesAsync();

            // Reload and return the full ExerciseViewModel
            var result = await _dbContext.Exercises
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
                    ImageUrl = e.ExerciseTemplate.ImageUrl,
                    TargetedMuscle = e.ExerciseTemplate.TargetedMuscle
                })
                .FirstOrDefaultAsync();
            return result!;
        }

        public async Task<bool> DeleteExerciseAsync(Guid exerciseId, Guid? userId)
        {
            try
            {
                // Find the exercise that belongs to the user's workout
                var exercise = await _dbContext.Exercises
                    .Include(e => e.Workout)
                    .FirstOrDefaultAsync(e => e.Id == exerciseId && e.Workout.UserId == userId && !e.IsDeleted);

                if (exercise == null)
                {
                    return false;
                }

                // Soft delete the exercise
                exercise.IsDeleted = true;
                await _dbContext.SaveChangesAsync();

                // Recalculate workout duration after deleting the exercise
                var workout = exercise.Workout;
                var remainingExercises = await _dbContext.Exercises
                    .Where(e => e.WorkoutId == workout.Id && !e.IsDeleted)
                    .ToListAsync();

                workout.Duration = CalculateWorkoutDuration(remainingExercises);
                await _dbContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error deleting exercise: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<ExerciseTemplateViewModel>> GetExerciseTemplatesAsync()
        {
            try
            {
                // Fetch the exercise templates from the database
                var templatesFromDb = await _dbContext
                    .ExerciseTemplates
                    .AsNoTracking()
                    .ToListAsync();

                // Map to ExerciseTemplateViewModel
                var templates = new List<ExerciseTemplateViewModel>();

                foreach (var template in templatesFromDb)
                {
                    templates.Add(new ExerciseTemplateViewModel
                    {
                        Id = template.Id,
                        Name = template.Name ?? "",
                        ImageUrl = template.ImageUrl ?? "",
                        TargetedMuscle = template.TargetedMuscle ?? ""
                    });
                }

                return templates;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it appropriately
                throw new InvalidOperationException("Error retrieving exercise templates", ex);
            }
        }

        public async Task<bool> VerifyWorkoutOwnershipAsync(Guid workoutId, Guid? userId)
        {
            if (userId == null)
                return false;
            
            // Check if the workout exists and belongs to the user
            return await 
                _dbContext
                .Workouts
                .AnyAsync(w => w.Id == workoutId 
                    && w.UserId == userId);
        }

        /// <summary>
        /// Calculates the total duration of a workout based on its exercises
        /// </summary>
        /// <param name="exercises">Collection of exercises</param>
        /// <returns>Total duration as TimeSpan</returns>
        private TimeSpan CalculateWorkoutDuration(IEnumerable<Exercise>? exercises)
        {
            if (exercises == null || !exercises.Any())
            {
                return TimeSpan.Zero;
            }

            return exercises
                .Where(e => !e.IsDeleted)
                .Aggregate(TimeSpan.Zero, (sum, exercise) => sum.Add(exercise.Duration));
        }
    }
}
