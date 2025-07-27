using System;

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
