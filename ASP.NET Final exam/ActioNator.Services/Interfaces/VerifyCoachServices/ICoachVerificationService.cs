using ActioNator.ViewModels.CoachVerification;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.VerifyCoachServices
{
    /// <summary>
    /// Interface for coach verification service
    /// </summary>
    public interface ICoachVerificationService
    {
        /// <summary>
        /// Gets all verification requests
        /// </summary>
        /// <returns>List of coach verification user view models</returns>
        Task<List<CoachVerificationUserViewModel>> GetAllVerificationRequestsAsync();

        /// <summary>
        /// Gets documents for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of coach document view models</returns>
        Task<List<CoachDocumentViewModel>> GetDocumentsForUserAsync(string userId);

        /// <summary>
        /// Approves a verification request
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ApproveVerificationAsync(string userId);

        /// <summary>
        /// Rejects a verification request
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Reason for rejection</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RejectVerificationAsync(string userId, string reason);
    }
}
