namespace ActioNator.Services.Exceptions
{
    /// <summary>
    /// Base exception for file-related operations in the service layer
    /// </summary>
    public abstract class FileServiceException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the FileServiceException class
        /// </summary>
        /// <param name="message">The error message</param>
        protected FileServiceException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileServiceException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        protected FileServiceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
