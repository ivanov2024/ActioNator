using ActioNator.Services.Models;
using Microsoft.AspNetCore.Http;

namespace ActioNator.Services.Interfaces.VerifyCoachServices
{
    /// <summary>
    /// Interface for handling coach verification document uploads with enterprise-grade security and validation
    /// </summary>
    public interface ICoachDocumentUploadService
    {
        /// <summary>
        /// Validates and processes uploaded coach verification documents with enhanced security and validation
        /// </summary>
        /// <param name="files">Collection of files to upload</param>
        /// <param name="userId">ID of the user uploading the documents</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the upload operation with detailed information</returns>
        Task<FileUploadResponseModel> ProcessUploadAsync(IFormFileCollection files, string userId, CancellationToken cancellationToken = default);
    }
}
