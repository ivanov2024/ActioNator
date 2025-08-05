namespace ActioNator.Services.Exceptions
{
    /// <summary>
    /// Exception thrown when file content type is invalid or doesn't match the actual content
    /// </summary>
    public class FileContentTypeException : FileValidationException
    {
        /// <summary>
        /// Initializes a new instance of the FileContentTypeException class
        /// </summary>
        /// <param name="message">The error message</param>
        public FileContentTypeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileContentTypeException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public FileContentTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

}
