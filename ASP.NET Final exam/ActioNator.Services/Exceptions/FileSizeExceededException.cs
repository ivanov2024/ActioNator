namespace ActioNator.Services.Exceptions
{
    /// <summary>
    /// Exception thrown when file size exceeds allowed limits
    /// </summary>
    public class FileSizeExceededException : FileValidationException
    {
        /// <summary>
        /// Initializes a new instance of the FileSizeExceededException class
        /// </summary>
        /// <param name="message">The error message</param>
        public FileSizeExceededException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileSizeExceededException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public FileSizeExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
