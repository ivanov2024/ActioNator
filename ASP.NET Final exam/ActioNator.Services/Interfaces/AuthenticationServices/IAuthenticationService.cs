using ActioNator.Data.Models;

namespace ActioNator.Services.Interfaces.AuthenticationServices
{
    /// <summary>
    /// Service interface for handling authentication-related operations
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user with the provided credentials
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        /// <param name="isPersistent">Whether the authentication should persist across browser sessions</param>
        /// <returns>Tuple containing success status and redirect path</returns>
        Task<(bool Succeeded, string RedirectPath, string ErrorMessage)> AuthenticateUserAsync(string email, string password, bool isPersistent);

        /// <summary>
        /// Determines the appropriate redirect path based on user's role
        /// </summary>
        /// <param name="user">The application user</param>
        /// <returns>The appropriate redirect path</returns>
        Task<string> GetRoleBasedRedirectPathAsync(ApplicationUser user);

        /// <summary>
        /// Registers a new user with the provided information
        /// </summary>
        /// <param name="firstName">User's first name</param>
        /// <param name="lastName">User's last name</param>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        /// <returns>Tuple containing success status, redirect path, and any error messages</returns>
        Task<(bool Succeeded, string RedirectPath, IEnumerable<string> ErrorMessages)> 
            RegisterUserAsync(string firstName, string lastName, string email, string password);

        /// <summary>
        /// Validates if the provided return URL is safe
        /// </summary>
        /// <param name="returnUrl">The URL to validate</param>
        /// <param name="defaultUrl">The default URL to use if the provided URL is invalid</param>
        /// <returns>A safe URL to redirect to</returns>
        string ValidateAndSanitizeReturnUrl(string returnUrl, string defaultUrl);
    }
}
