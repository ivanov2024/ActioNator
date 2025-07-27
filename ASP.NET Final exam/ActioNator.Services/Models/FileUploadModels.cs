using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    
    /// <summary>
    /// Response model for file upload operations
    /// </summary>
    public class FileUploadResponse : BaseResponseModel
    {
        /// <summary>
        /// List of uploaded file paths
        /// </summary>
        public List<UploadedFileInfo> Files { get; set; } = new List<UploadedFileInfo>();
        
        /// <summary>
        /// Error details if the operation failed
        /// </summary>
        public ErrorDetails? Error { get; set; }
        
        /// <summary>
        /// Creates a successful response
        /// </summary>
        /// <param name="message">Success message</param>
        /// <param name="files">List of uploaded file information</param>
        /// <returns>FileUploadResponse instance</returns>
        public static FileUploadResponse CreateSuccess(string message, List<UploadedFileInfo> files)
        {
            return new FileUploadResponse
            {
                Success = true,
                Message = message,
                Files = files ?? new List<UploadedFileInfo>()
            };
        }
        
        /// <summary>
        /// Creates a failure response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorDetails">Detailed error information</param>
        /// <returns>FileUploadResponse instance</returns>
        public static FileUploadResponse CreateFailure(string message, ErrorDetails? errorDetails = null)
        {
            return new FileUploadResponse
            {
                Success = false,
                Message = message,
                Error = errorDetails ?? new ErrorDetails { ErrorType = "UnknownError", ErrorMessage = message }
            };
        }
    }
    
    /// <summary>
    /// Information about an uploaded file
    /// </summary>
    public class UploadedFileInfo
    {
        /// <summary>
        /// Original file name
        /// </summary>
        public required string OriginalFileName { get; set; }
        
        /// <summary>
        /// Stored file name
        /// </summary>
        public required string StoredFileName { get; set; }
        
        /// <summary>
        /// Relative path to the file
        /// </summary>
        public required string FilePath { get; set; }
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// File content type
        /// </summary>
        public required string ContentType { get; set; }
    }
    
    /// <summary>
    /// Detailed error information
    /// </summary>
    public class ErrorDetails
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
        public static ErrorDetails FromException(Exception exception)
        {
            if (exception == null)
            {
                return new ErrorDetails
                {
                    ErrorType = "UnknownError",
                    ErrorMessage = "An unknown error occurred"
                };
            }
            
            var errorDetails = new ErrorDetails
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
