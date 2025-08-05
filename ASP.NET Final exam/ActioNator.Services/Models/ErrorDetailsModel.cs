namespace ActioNator.Services.Models
{
    /// <summary>
    /// Detailed error information
    /// </summary>
    public class ErrorDetailsModel
    {
        /// <summary>
        /// Type of error
        /// </summary>
        public required string ErrorType { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public required string ErrorMessage { get; set; }

        /// <summary>
        /// Additional error details
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Creates error details from an exception
        /// </summary>
        /// <param name="exception">Exception to create error details from</param>
        /// <returns>ErrorDetails instance</returns>
        public static ErrorDetailsModel FromException(Exception exception)
        {
            if (exception == null)
            {
                return new ErrorDetailsModel
                {
                    ErrorType = "UnknownError",
                    ErrorMessage = "An unknown error occurred"
                };
            }

            var errorDetails = new ErrorDetailsModel
            {
                ErrorType = exception.GetType().Name,
                ErrorMessage = exception.Message
            };

            // Add stack trace in development environments only
#if DEBUG
            errorDetails.AdditionalInfo["StackTrace"] = exception.StackTrace;
#endif

            return errorDetails;
        }
    }
}
