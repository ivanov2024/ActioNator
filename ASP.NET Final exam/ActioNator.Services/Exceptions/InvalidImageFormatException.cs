namespace ActioNator.Services.Exceptions
{
    /// <summary>
    /// Thrown when an uploaded image fails validation (type, extension, headers, or corruption).
    /// </summary>
    public class InvalidImageFormatException : FileValidationException
    {
        public InvalidImageFormatException(string message) : base(message)
        {
        }

        public InvalidImageFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
