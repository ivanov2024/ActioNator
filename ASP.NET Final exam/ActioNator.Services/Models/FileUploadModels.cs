namespace ActioNator.Services.Models
{
    /// <summary>
    /// Response model for file upload operations
    /// </summary>
    public class FileUploadResponse : BaseResponseModel
    {
        /// <summary>
        /// List of uploaded file paths
        /// </summary>
        public List<UploadedFileInfo> Files { get; set; } 
            = new List<UploadedFileInfo>();
        
        /// <summary>
        /// Error details if the operation failed
        /// </summary>
        public ErrorDetailsModel? Error { get; set; }
        
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
        public static FileUploadResponse CreateFailure(string message, ErrorDetailsModel? errorDetails = null)
        {
            return new FileUploadResponse
            {
                Success = false,
                Message = message,
                Error = errorDetails ?? new ErrorDetailsModel { ErrorType = "UnknownError", ErrorMessage = message }
            };
        }
    }
}
