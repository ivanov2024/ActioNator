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
                    // First try standard validation
                    try
                    {
                        await _antiforgery.ValidateRequestAsync(context.HttpContext);
                        _logger.LogInformation("Anti-forgery token validated via standard method");
                        return; // If standard validation passes, we're done
                    }
                    catch (AntiforgeryValidationException ex)
                    {
                        _logger.LogInformation("Standard anti-forgery validation failed: {Message}, trying JSON body extraction", ex.Message);
                        // Continue to JSON body extraction
                    }

                    // If standard validation fails, try to extract from JSON body
                    if (context.HttpContext.Request.HasJsonContentType())
                    {
                        context.HttpContext.Request.EnableBuffering();
                        using (var reader = new StreamReader(context.HttpContext.Request.Body, System.Text.Encoding.UTF8, true, 1024, true))
                        {
                            context.HttpContext.Request.Body.Position = 0;
                            var body = await reader.ReadToEndAsync();
                            context.HttpContext.Request.Body.Position = 0;

                            _logger.LogInformation("Request body: {Body}", body);

                            if (!string.IsNullOrEmpty(body))
                            {
                                try
                                {
                                    var jsonDocument = JsonDocument.Parse(body);
                                    if (jsonDocument.RootElement.TryGetProperty("__RequestVerificationToken", out var tokenElement))
                                    {
                                        var token = tokenElement.GetString();
                                        if (!string.IsNullOrEmpty(token))
                                        {
                                            // Add the token to the headers for validation
                                            context.HttpContext.Request.Headers["X-CSRF-TOKEN"] = token;
                                            
                                            // Try validation again
                                            await _antiforgery.ValidateRequestAsync(context.HttpContext);
                                            _logger.LogInformation("Anti-forgery token validated from JSON body");
                                            return;
                                        }
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    _logger.LogError(ex, "Error parsing JSON body for anti-forgery token");
                                }
                            }
                        }
                    }

                    // If we get here, validation failed
                    _logger.LogWarning("Anti-forgery token validation failed");
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
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
