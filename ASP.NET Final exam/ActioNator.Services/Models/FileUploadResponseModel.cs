namespace ActioNator.Services.Models
{
    /// <summary>
    /// Response model for file upload operations
    /// </summary>
    public class FileUploadResponseModel : BaseResponseModel
    {
        /// <summary>
        /// List of uploaded file paths
        /// </summary>
        public List<UploadedFileInfoModel> Files { get; set; } 
            = new List<UploadedFileInfoModel>();
        
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
        public static FileUploadResponseModel CreateSuccess(string message, List<UploadedFileInfoModel> files)
        {
            return new FileUploadResponseModel
            {
                Success = true,
                Message = message,
                Files = files ?? new List<UploadedFileInfoModel>()
            };
        }
        
        /// <summary>
        /// Creates a failure response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorDetails">Detailed error information</param>
        /// <returns>FileUploadResponse instance</returns>
        public static FileUploadResponseModel CreateFailure(string message, ErrorDetailsModel? errorDetails = null)
        {
            return new FileUploadResponseModel
            {
                Success = false,
                Message = message,
                Error = errorDetails ?? new ErrorDetailsModel { ErrorType = "UnknownError", ErrorMessage = message }
            };
        }
    }
}
