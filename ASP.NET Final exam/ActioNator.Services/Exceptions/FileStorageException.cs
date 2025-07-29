namespace ActioNator.Services.Exceptions
{
    /// <summary>
    /// Exception thrown when file storage operations fail
    /// </summary>
    public class FileStorageException : FileServiceException
    {
        /// <summary>
        /// Initializes a new instance of the FileStorageException class
        /// </summary>
        /// <param name="message">The error message</param>
        public FileStorageException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FileStorageException class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public FileStorageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
