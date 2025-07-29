namespace ActioNator.Services.Exceptions
{

    /// <summary>
    /// Exception thrown when file name contains invalid characters or patterns
    /// </summary>
    public class FileNameValidationException : FileValidationException
    {
        /// <summary>
        /// Initializes a new instance of the FileNameValidationException class
        /// </summary>
        /// <param name="message">The error message</param>
        public FileNameValidationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileNameValidationException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public FileNameValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
