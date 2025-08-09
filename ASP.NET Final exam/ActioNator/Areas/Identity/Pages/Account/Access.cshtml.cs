using ActioNator.Services.Interfaces.AuthenticationServices;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ActioNator.GCommon;
using static ActioNator.GCommon.ValidationConstants.ApplicationUser;
using ActioNator.Services.Interfaces.InputSanitizationService;
using System.Reflection;

namespace ActioNator.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Razor Page model for handling user authentication (login and registration)
    /// </summary>
    [AllowAnonymous]
    public class AccessModel : PageModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AccessModel> _logger;
        private readonly IInputSanitizationService _inputSanitizationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessModel"/> class.
        /// </summary>
        /// <param name="authService">The authentication service</param>
        /// <param name="logger">The logger</param>
        public AccessModel(
            IAuthenticationService authService,
            ILogger<AccessModel> logger, IInputSanitizationService inputSanitizationService)
        {
            _authService = authService 
                ?? throw new ArgumentNullException(nameof(authService));

            _logger = logger 
                ?? throw new ArgumentNullException(nameof(logger));

            _inputSanitizationService = inputSanitizationService
                ?? throw new ArgumentNullException(nameof(inputSanitizationService));
        }


        /// <summary>
        /// Registration input model
        /// </summary>
        [BindProperty]
        public RegisterInputModel RegisterInput { get; set; } 
            = new();

        /// <summary>
        /// Login input model
        /// </summary>
        [BindProperty]
        public LoginInputModel LoginInput { get; set; } 
            = new();

        /// <summary>
        /// Return URL after successful authentication
        /// </summary>
        public string ReturnUrl { get; set; } = "";

        /// <summary>
        /// Handles GET requests to the page
        /// </summary>
        /// <param name="returnUrl">URL to redirect to after authentication</param>
        public void OnGet(string returnUrl = null)
        {
            // Redirect authenticated users
            if (User.Identity?.IsAuthenticated == true)
            {
                string role 
                    = User.IsInRole(RoleConstants.Admin) ? "Admin" 
                    : User.IsInRole(RoleConstants.Coach) ? "Coach" 
                    : "User";

                ReturnUrl = role switch
                {
                    "Admin" => "/Admin/Home/Index",
                    "Coach" => "/Coach/Home/Index",
                    _ => "/User/Home/Index"
                };

                Response.Redirect(ReturnUrl);
                return;
            }

            // Validate and sanitize return URL
            ReturnUrl = _authService.ValidateAndSanitizeReturnUrl(returnUrl, ReturnUrl);
        }

        /// <summary>
        /// Handles user registration
        /// </summary>
        /// <param name="returnUrl">URL to redirect to after successful registration</param>
        /// <returns>IActionResult</returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRegisterAsync(string returnUrl = null)
        {
            // Clear ModelState errors for the login form since we're processing registration
            ClearModelStateForPrefix("LoginInput");

            try
            {
                // Validate and sanitize return URL
                ReturnUrl 
                    = _authService
                    .ValidateAndSanitizeReturnUrl(returnUrl, ReturnUrl);

                // Validate model
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                foreach (PropertyInfo prop in typeof(RegisterInputModel).GetProperties())
                {
                    // Sanitize each property value
                    string value 
                        = prop
                        .GetValue(RegisterInput)?
                        .ToString() ?? string.Empty;

                    string sanitizedValue = 
                        _inputSanitizationService
                        .SanitizeString(value);

                    prop
                        .SetValue(RegisterInput, sanitizedValue);
                }

                // Register user
                var (succeeded, redirectPath, errorMessages) 
                    = await _authService
                    .RegisterUserAsync(
                    RegisterInput.FirstName,
                    RegisterInput.LastName,
                    RegisterInput.Email,
                    RegisterInput.Password);

                if (succeeded)
                {
                    _logger
                        .LogInformation("User registered successfully");
                    return LocalRedirect(redirectPath);
                }

                // Add errors to ModelState
                foreach (var error in errorMessages)
                {
                    ModelState
                        .AddModelError(string.Empty, error);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error during user registration");
                ModelState
                    .AddModelError(string.Empty, "An unexpected error occurred during registration.");
                return Page();
            }
        }

        /// <summary>
        /// Handles user login
        /// </summary>
        /// <param name="returnUrl">URL to redirect to after successful login</param>
        /// <returns>IActionResult</returns>
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLoginAsync(string returnUrl = null)
        {
            // Clear ModelState errors for the registration form since we're processing login
            ClearModelStateForPrefix("RegisterInput");

            try
            {
                // Validate and sanitize return URL
                ReturnUrl 
                    = _authService
                    .ValidateAndSanitizeReturnUrl(returnUrl, ReturnUrl);

                // Validate model
                if (!ModelState.IsValid)
                {
                    return Page();
                }

                foreach (PropertyInfo prop in typeof(LoginInputModel).GetProperties())
                {
                    // Sanitize each property value
                    string value
                        = prop
                        .GetValue(LoginInput)?
                        .ToString() ?? string.Empty;

                    string sanitizedValue =
                        _inputSanitizationService
                        .SanitizeString(value);

                    prop
                        .SetValue(LoginInput, sanitizedValue);
                }

                // Authenticate user
                var (succeeded, redirectPath, errorMessage) 
                    = await _authService
                    .AuthenticateUserAsync(
                    LoginInput.Email,
                    LoginInput.Password,
                    isPersistent: false);

                if (succeeded)
                {
                    _logger
                        .LogInformation("User logged in successfully");
                    return LocalRedirect(redirectPath);
                }

                // Add error to ModelState
                ModelState
                    .AddModelError(string.Empty, errorMessage);
                return Page();
            }
            catch (Exception ex)
            {
                _logger
                    .LogError(ex, "Error during user login");
                ModelState
                    .AddModelError(string.Empty, "An unexpected error occurred during login.");
                return Page();
            }
        }

        /// <summary>
        /// Model for user registration input
        /// </summary>
        public class RegisterInputModel
        {
            /// <summary>
            /// User's first name
            /// </summary>
            [Required(ErrorMessage = "First name is required.")]
            [Display(Name = "First Name")]
            [MinLength(FirstNameMinLength, ErrorMessage = "First name must be at least {1} characters.")]
            [MaxLength(FirstNameMaxLength, ErrorMessage = "First name cannot exceed {1} characters.")]
            [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First name can only contain letters.")]
            public string FirstName { get; set; } = null!;

            /// <summary>
            /// User's last name
            /// </summary>
            [Required(ErrorMessage = "Last name is required.")]
            [Display(Name = "Last Name")]
            [MinLength(LastNameMinLength, ErrorMessage = "Last name must be at least {1} characters.")]
            [MaxLength(LastNameMaxLength, ErrorMessage = "Last name cannot exceed {1} characters.")]
            [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "Last name can only contain letters.")]
            public string LastName { get; set; } = null!;

            /// <summary>
            /// User's email address
            /// </summary>
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = null!;

            /// <summary>
            /// User's password
            /// </summary>
            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
            public string Password { get; set; } = null!;

            /// <summary>
            /// Password confirmation
            /// </summary>
            [Required(ErrorMessage = "Password confirmation is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = null!;
        }

        /// <summary>
        /// Model for user login input
        /// </summary>
        public class LoginInputModel
        {
            /// <summary>
            /// User's email address
            /// </summary>
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            [Display(Name = "Email")]
            public string Email { get; set; } = null!;

            /// <summary>
            /// User's password
            /// </summary>
            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = null!;
        }

        #region Private Helper Methods
        /// <summary>
        /// Clears ModelState errors for properties with a specific prefix
        /// </summary>
        /// <param name="prefix">The prefix to clear errors for</param>
        private void ClearModelStateForPrefix(string prefix)
        {
            // Find all keys in ModelState that start with the given prefix
            List<string>? keysToRemove = [];
            
            // Explicitly iterate through all keys to find matches
            foreach (var key in ModelState.Keys)
            {
                if (key != null && key
                    .StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    keysToRemove.Add(key);
                }
            }

            // Remove each key from ModelState
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
            
            // If somehow something goes wrong: directly clear ModelState.IsValid
            if (!ModelState.IsValid && keysToRemove.Count == 0)
            {
                // We have to force ModelState
                // to be valid for this handler
                ModelState.Clear();
            }
        }

        #endregion
    }
}
