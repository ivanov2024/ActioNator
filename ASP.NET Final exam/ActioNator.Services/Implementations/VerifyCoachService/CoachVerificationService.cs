using ActioNator.Data;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.CoachVerification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ActioNator.Services.Implementations.VerifyCoach
{
    /// <summary>
    /// Implementation of the coach verification service
    /// </summary>
    public class CoachVerificationService : ICoachVerificationService
    {
        private readonly ActioNatorDbContext _dbContext;
        private readonly ILogger<CoachVerificationService> _logger;

        public CoachVerificationService(ActioNatorDbContext dbContext, ILogger<CoachVerificationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Gets all verification requests
        /// </summary>
        /// <returns>List of coach verification user view models</returns>
        public async Task<List<CoachVerificationUserViewModel>> GetAllVerificationRequestsAsync()
        {
            try
            {
                // In a real application, we would query for users with pending coach verification
                // For now, we'll return users with a CoachDegreeFilePath but not yet verified
                var users = await _dbContext.Users
                    .Where(u => !string.IsNullOrEmpty(u.CoachDegreeFilePath) && !u.IsVerifiedCoach)
                    .Select(u => new CoachVerificationUserViewModel
                    {
                        UserId = u.Id.ToString(),
                        Documents = new List<CoachDocumentViewModel>
                        {
                            new CoachDocumentViewModel
                            {
                                FileName = Path.GetFileName(u.CoachDegreeFilePath),
                                RelativePath = u.CoachDegreeFilePath,
                                FileType = Path.GetExtension(u.CoachDegreeFilePath).ToLower() == ".pdf" ? "pdf" : "image"
                            }
                        }
                    })
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting verification requests");
                return new List<CoachVerificationUserViewModel>();
            }
        }

        /// <summary>
        /// Gets documents for a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>List of coach document view models</returns>
        public async Task<List<CoachDocumentViewModel>> GetDocumentsForUserAsync(string userId)
        {
            try
            {
                // Convert string userId to Guid
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    _logger.LogError("Invalid user ID format: {UserId}", userId);
                    return new List<CoachDocumentViewModel>();
                }

                var user = await _dbContext.Users.FindAsync(userGuid);
                if (user == null || string.IsNullOrEmpty(user.CoachDegreeFilePath))
                {
                    return new List<CoachDocumentViewModel>();
                }

                // Return the coach degree file as a document
                return new List<CoachDocumentViewModel>
                {
                    new CoachDocumentViewModel
                    {
                        FileName = Path.GetFileName(user.CoachDegreeFilePath),
                        RelativePath = user.CoachDegreeFilePath,
                        FileType = Path.GetExtension(user.CoachDegreeFilePath).ToLower() == ".pdf" ? "pdf" : "image"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents for user {UserId}", userId);
                return new List<CoachDocumentViewModel>();
            }
        }

        /// <summary>
        /// Approves a verification request
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ApproveVerificationAsync(string userId)
        {
            try
            {
                // Convert string userId to Guid
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    _logger.LogError("Invalid user ID format: {UserId}", userId);
                    return false;
                }

                var user = await _dbContext.Users.FindAsync(userGuid);
                if (user == null)
                {
                    return false;
                }

                user.IsVerifiedCoach = true;
                
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving verification for user {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Rejects a verification request
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="reason">Reason for rejection</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> RejectVerificationAsync(string userId, string reason)
        {
            try
            {
                // Convert string userId to Guid
                if (!Guid.TryParse(userId, out Guid userGuid))
                {
                    _logger.LogError("Invalid user ID format: {UserId}", userId);
                    return false;
                }

                var user = await _dbContext.Users.FindAsync(userGuid);
                if (user == null)
                {
                    return false;
                }

                // Clear the coach degree file path
                user.CoachDegreeFilePath = null;
                
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting verification for user {UserId}", userId);
                return false;
            }
        }
    }
}
