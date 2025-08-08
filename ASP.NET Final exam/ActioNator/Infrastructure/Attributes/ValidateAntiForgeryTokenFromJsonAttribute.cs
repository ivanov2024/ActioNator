using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace ActioNator.Infrastructure.Attributes
{
    /// <summary>
    /// Custom attribute that validates anti-forgery tokens for JSON requests
    /// </summary>
    public class ValidateAntiForgeryTokenFromJsonAttribute : TypeFilterAttribute
    {
        public ValidateAntiForgeryTokenFromJsonAttribute() : base(typeof(ValidateAntiForgeryTokenFromJsonFilter))
        {
        }

        private class ValidateAntiForgeryTokenFromJsonFilter : IAsyncAuthorizationFilter
        {
            private readonly IAntiforgery _antiforgery;
            private readonly ILogger<ValidateAntiForgeryTokenFromJsonFilter> _logger;

            public ValidateAntiForgeryTokenFromJsonFilter(
                IAntiforgery antiforgery,
                ILogger<ValidateAntiForgeryTokenFromJsonFilter> logger)
            {
                _antiforgery = antiforgery;
                _logger = logger;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                try
                {
                    // Always check for X-CSRF-TOKEN header for JSON requests
                    if (context.HttpContext.Request.HasJsonContentType())
                    {
                        var csrfHeader = context.HttpContext.Request.Headers["X-CSRF-TOKEN"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(csrfHeader))
                        {
                            context.HttpContext.Request.Headers["RequestVerificationToken"] = csrfHeader;
                            try
                            {
                                await _antiforgery.ValidateRequestAsync(context.HttpContext);
                                _logger.LogInformation("Anti-forgery token validated via X-CSRF-TOKEN header");
                                return;
                            }
                            catch (AntiforgeryValidationException ex)
                            {
                                _logger.LogWarning("Anti-forgery validation failed via X-CSRF-TOKEN header: {Message}", ex.Message);
                                context.Result = new JsonResult(new { success = false, message = "Invalid or expired anti-forgery token." }) { StatusCode = StatusCodes.Status403Forbidden };
                                return;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Missing X-CSRF-TOKEN header for JSON request");
                            context.Result = new JsonResult(new { success = false, message = "Missing anti-forgery token. Please refresh and try again." }) { StatusCode = StatusCodes.Status403Forbidden };
                            return;
                        }
                    }
                    else
                    {
                        // Fallback to standard validation for non-JSON requests
                        try
                        {
                            await _antiforgery.ValidateRequestAsync(context.HttpContext);
                            _logger.LogInformation("Anti-forgery token validated via standard method");
                            return;
                        }
                        catch (AntiforgeryValidationException ex)
                        {
                            _logger.LogWarning("Standard anti-forgery validation failed: {Message}", ex.Message);
                            context.Result = new JsonResult(new { success = false, message = "Invalid or expired anti-forgery token." }) { StatusCode = StatusCodes.Status403Forbidden };
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during anti-forgery token validation");
                    context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            }
        }
    }
}
