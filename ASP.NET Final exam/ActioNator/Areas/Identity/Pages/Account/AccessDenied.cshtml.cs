using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ActioNator.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Page model for handling access denied scenarios
    /// </summary>
    public class AccessDeniedModel : PageModel
    {
        private readonly ILogger<AccessDeniedModel> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessDeniedModel"/> class.
        /// </summary>
        /// <param name="logger">The logger</param>
        public AccessDeniedModel(ILogger<AccessDeniedModel> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// The URL to return to after viewing the access denied page
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/";

        /// <summary>
        /// Handles GET requests to the page
        /// </summary>
        /// <param name="returnUrl">URL to return to</param>
        public void OnGet(string returnUrl = null)
        {
            _logger.LogWarning("Access denied page accessed with return URL: {ReturnUrl}", returnUrl);
            
            // Sanitize the return URL to prevent open redirect vulnerabilities
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                ReturnUrl = returnUrl;
            }
            else
            {
                // Default to home page if return URL is invalid
                ReturnUrl = "/";
            }
        }
    }
}
