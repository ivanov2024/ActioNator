namespace ActioNator.Services.Exceptions
{
    /// <summary>
    /// Exception thrown when file validation fails
    /// </summary>
    public class FileValidationException : FileServiceException
    {
        /// <summary>
        /// Initializes a new instance of the FileValidationException class
        /// </summary>
        /// <param name="message">The error message</param>
        public FileValidationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileValidationException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public FileValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
