using System;
using System.Text.Json.Serialization;

namespace ActioNator.ViewModels.Workout
{
    /// <summary>
    /// View model for exercise deletion operations
    /// </summary>
    public class ExerciseDeleteViewModel
    {
        /// <summary>
        /// The unique identifier of the exercise to delete
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
    }
}
