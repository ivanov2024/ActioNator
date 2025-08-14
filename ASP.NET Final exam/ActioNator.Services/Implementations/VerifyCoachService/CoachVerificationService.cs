using ActioNator.Data;
using ActioNator.Data.Models.Enums;
using ActioNator.Services.Interfaces.VerifyCoachServices;
using ActioNator.ViewModels.CoachVerification;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _env;

        public CoachVerificationService(ActioNatorDbContext dbContext, ILogger<CoachVerificationService> logger, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Gets the number of users awaiting coach verification.
        /// </summary>
        /// <returns>Count of pending coach verifications</returns>
        public async Task<int> GetPendingVerificationsCountAsync()
        {
            try
            {
                return await _dbContext.Users
                    .Where(u => !u.IsDeleted)
                    .Where(u => !u.IsVerifiedCoach)
                    .Where(u => !string.IsNullOrEmpty(u.CoachDegreeFilePath))
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting pending coach verifications");
                return 0;
            }
        }

        /// <summary>
        /// Gets all verification requests
        /// </summary>
        /// <returns>List of coach verification user view models</returns>
        public async Task<List<CoachVerificationUserViewModel>> GetAllVerificationRequestsAsync()
        {
            try
            {
                // Enumerate user folders under App_Data/coach-verifications and build requests from the filesystem
                string verificationsRoot = Path.Combine(_env.ContentRootPath, "App_Data", "coach-verifications");

                var results = new List<CoachVerificationUserViewModel>();

                if (!Directory.Exists(verificationsRoot))
                {
                    _logger.LogInformation("Coach verifications root does not exist: {Root}", verificationsRoot);
                    return results;
                }

                foreach (var dir in Directory.EnumerateDirectories(verificationsRoot))
                {
                    string folderName = Path.GetFileName(dir);
                    if (!Guid.TryParse(folderName, out Guid userGuid))
                    {
                        _logger.LogWarning("Skipping non-GUID folder in coach-verifications: {Folder}", folderName);
                        continue;
                    }

                    var user = await _dbContext.Users.FindAsync(userGuid);
                    if (user == null)
                    {
                        continue;
                    }

                    // Skip users already approved as coach
                    if (user.IsVerifiedCoach)
                    {
                        continue;
                    }

                    var docs = await GetDocumentsForUserAsync(folderName);
                    if (docs.Any())
                    {
                        results.Add(new CoachVerificationUserViewModel
                        {
                            UserId = folderName,
                            Documents = docs
                        });
                    }
                }

                return results;
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
                if (user == null)
                {
                    return new List<CoachDocumentViewModel>();
                }

                // Enumerate files under App_Data/coach-verifications/{userId}/
                // Resolve absolute directory from ContentRoot (Directory.GetCurrentDirectory in ASP.NET Core)
                string userFolder = Path.Combine("App_Data", "coach-verifications", userId);
                string absoluteFolder = Path.Combine(_env.ContentRootPath, userFolder);

                // Resolve to a normalized relative path (we will return relative paths for secure controller streaming)
                // If directory doesn't exist, return empty list
                var documents = new List<CoachDocumentViewModel>();
                try
                {
                    if (Directory.Exists(absoluteFolder))
                    {
                        foreach (var file in Directory.EnumerateFiles(absoluteFolder))
                        {
                            var ext = Path.GetExtension(file).ToLowerInvariant();
                            string fileType = ext == ".pdf" ? "pdf" :
                                (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".gif" || ext == ".webp" ? "image" : "other");
                            documents.Add(new CoachDocumentViewModel
                            {
                                FileName = Path.GetFileName(file),
                                RelativePath = Path.Combine(userFolder, Path.GetFileName(file)),
                                FileType = fileType
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enumerating verification documents for user {UserId} in {Folder}", userId, userFolder);
                }

                return documents;
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
                user.Role = Role.Coach;
                // Clear stored degree path since verification is complete
                user.CoachDegreeFilePath = null;

                await _dbContext.SaveChangesAsync();

                // Best-effort: delete verification folder to remove from pending list
                try
                {
                    string userFolder = Path.Combine(_env.ContentRootPath, "App_Data", "coach-verifications", userId);
                    if (Directory.Exists(userFolder))
                    {
                        Directory.Delete(userFolder, recursive: true);
                        _logger.LogInformation("Deleted verification folder for approved coach {UserId}", userId);
                    }
                }
                catch (Exception delEx)
                {
                    _logger.LogWarning(delEx, "Failed to delete verification folder for approved coach {UserId}", userId);
                }

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

                // Clear the coach degree file path flag
                user.CoachDegreeFilePath = null;

                // Delete the verification documents folder from disk
                try
                {
                    string userFolder = Path.Combine(_env.ContentRootPath, "App_Data", "coach-verifications", userId);
                    if (Directory.Exists(userFolder))
                    {
                        Directory.Delete(userFolder, recursive: true);
                        _logger.LogInformation("Deleted coach verification folder for user {UserId}: {Folder}", userId, userFolder);
                    }
                }
                catch (Exception delEx)
                {
                    // Log but continue - we still save the DB changes and return false to indicate partial failure
                    _logger.LogError(delEx, "Error deleting verification folder for user {UserId}", userId);
                }

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
