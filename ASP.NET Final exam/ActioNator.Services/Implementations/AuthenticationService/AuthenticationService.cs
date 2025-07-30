using ActioNator.Services.Interfaces.AuthenticationServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ActioNator.Data.Models;
using ActioNator.GCommon;

namespace ActioNator.Services.Implementations.AuthenticationService
{
    /// <summary>
    /// Implementation of the authentication service
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<AuthenticationService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userStore = userStore;
            _emailStore = GetEmailStore(userStore);
            _logger = logger;
        }

        public async Task<(bool Succeeded, string RedirectPath, string ErrorMessage)> AuthenticateUserAsync(
            string email,
            string password,
            bool isPersistent)
        {
            try
            {
                // Find user by email
                ApplicationUser? user 
                    = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger
                        .LogWarning("Login attempt failed for non-existent email: {Email}", email);
                    return (false, string.Empty, "Invalid login credentials.");
                }

                // Attempt to sign in
                SignInResult result 
                    = await _signInManager
                    .PasswordSignInAsync(
                    user.UserName!,
                    password,
                    isPersistent,
                    lockoutOnFailure: true); // Enable account lockout for brute force protection

                if (result.Succeeded)
                {
                    _logger
                        .LogInformation("User {UserName} logged in successfully", user.UserName);
                    string redirectPath 
                        = await GetRoleBasedRedirectPathAsync(user);
                    return (true, redirectPath, string.Empty);
                }

                if (result.IsLockedOut)
                {
                    _logger
                        .LogWarning("User {UserName} account locked out", user.UserName);
                    return (false, string.Empty, "Account is temporarily locked. Please try again later.");
                }

                _logger
                    .LogWarning("Invalid login attempt for user {UserName}", user.UserName);
                return (false, string.Empty, "Invalid login credentials.");
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error during authentication");
                return (false, string.Empty, "An error occurred during login. Please try again later.");
            }
        }

        public async Task<string> GetRoleBasedRedirectPathAsync(ApplicationUser user)
        {
            ArgumentNullException.ThrowIfNull(user);

            // Check for Administrator role
            if (await _userManager
                .IsInRoleAsync(user, RoleConstants.Admin))
            {
                return RedirectPathConstants.AdminHome;
            }

            // Check for Coach role
            if (await _userManager
                .IsInRoleAsync(user, RoleConstants.Coach))
            {
                return RedirectPathConstants.CoachHome;
            }

            // Default to User role
            return RedirectPathConstants.UserHome;
        }

        public async Task<(bool Succeeded, string RedirectPath, IEnumerable<string> ErrorMessages)> RegisterUserAsync(
            string firstName,
            string lastName,
            string email,
            string password)
        {
            try
            {
                // Create new user
                ApplicationUser user = CreateUser();

                await _userStore
                    .SetUserNameAsync(user, firstName + lastName, CancellationToken.None);
                await _emailStore
                    .SetEmailAsync(user, email, CancellationToken.None);

                user.FirstName = firstName;
                user.LastName = lastName;

                // Create user with password
                IdentityResult result 
                    = await _userManager
                    .CreateAsync(user, password);

                if (result.Succeeded)
                {
                    _logger
                        .LogInformation("User {UserName} created successfully", user.UserName);

                    // Add default User role
                    await _userManager
                        .AddToRoleAsync(user, RoleConstants.User);

                    // Sign in the user
                    await _signInManager
                        .SignInAsync(user, isPersistent: false);

                    // Get role-based redirect path
                    string redirectPath 
                        = await GetRoleBasedRedirectPathAsync(user);

                    return (true, redirectPath, Array.Empty<string>());
                }

                _logger
                    .LogWarning("User registration failed: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));

                return (false, string.Empty, result.Errors.Select(e => e.Description));
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error during user registration");
                return (false, string.Empty, new[] { "An error occurred during registration. Please try again later." });
            }
        }

        public string ValidateAndSanitizeReturnUrl(string returnUrl, string defaultUrl)
        {
            // Check if URL is null, empty, or not a local URL
            if (string.IsNullOrEmpty(returnUrl) 
                || 
                !IsLocalUrl(returnUrl))
            {
                return defaultUrl;
            }

            return returnUrl;
        }

        #region Helper Private Methods  
        private static bool IsLocalUrl(string url)
        {
            // Check if URL is relative (starts with / or ~/)
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            // Check if URL is relative
            if (url[0] == '/' || url.Length > 1 && url[0] == '~' && url[1] == '/')
            {
                return true;
            }

            // Check if URL is absolute but local
            if (url.Length > 1 && url.Contains("://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return false;
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error creating user instance");
                throw new InvalidOperationException(
                    $"Cannot create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor.",
                    ex);
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore(IUserStore<ApplicationUser> store)
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The user store does not support email.");
            }

            return (IUserEmailStore<ApplicationUser>)store;
        }

        #endregion
    }
}
