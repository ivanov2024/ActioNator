namespace ActioNator.Services.Models
{
    /// <summary>
    /// Base response model with common properties
    /// </summary>
    public abstract class BaseResponseModel
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result of the operation
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Timestamp when the response was created
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

}
