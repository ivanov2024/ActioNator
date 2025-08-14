using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;

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
            private readonly AntiforgeryOptions _options;

            public ValidateAntiForgeryTokenFromJsonFilter(
                IAntiforgery antiforgery,
                ILogger<ValidateAntiForgeryTokenFromJsonFilter> logger,
                IOptions<AntiforgeryOptions> options)
            {
                _antiforgery = antiforgery;
                _logger = logger;
                _options = options.Value;
            }

            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                try
                {
                    var httpContext = context.HttpContext;
                    var request = httpContext.Request;

                    // JSON requests: prefer header, but support JSON body too
                    if (request.HasJsonContentType())
                    {
                        string? token = request.Headers["X-CSRF-TOKEN"].FirstOrDefault();
                        if (string.IsNullOrEmpty(token))
                        {
                            token = request.Headers["RequestVerificationToken"].FirstOrDefault();
                        }
                        if (string.IsNullOrEmpty(token))
                        {
                            token = request.Headers["CSRF-TOKEN"].FirstOrDefault();
                        }

                        // If not present in headers, try to read from JSON body
                        if (string.IsNullOrEmpty(token))
                        {
                            try
                            {
                                // Enable buffering so the body can be read again by model binding
                                request.EnableBuffering();

                                // Ensure position at start before reading
                                if (request.Body.CanSeek)
                                {
                                    request.Body.Position = 0;
                                }

                                using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                                var body = await reader.ReadToEndAsync();

                                // Rewind for the model binder
                                if (request.Body.CanSeek)
                                {
                                    request.Body.Position = 0;
                                }

                                if (!string.IsNullOrWhiteSpace(body))
                                {
                                    try
                                    {
                                        using var doc = JsonDocument.Parse(body);
                                        var root = doc.RootElement;
                                        // Common property names that might carry the antiforgery token
                                        var candidates = new[]
                                        {
                                            "__RequestVerificationToken",
                                            "RequestVerificationToken",
                                            "X-CSRF-TOKEN",
                                            "AntiForgeryToken",
                                            "AntiForgery"
                                        };
                                        foreach (var name in candidates)
                                        {
                                            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
                                            {
                                                token = prop.GetString();
                                                break;
                                            }
                                        }
                                    }
                                    catch (JsonException jex)
                                    {
                                        _logger.LogDebug(jex, "Request body is not valid JSON while searching for antiforgery token");
                                    }
                                }
                            }
                            catch (Exception readEx)
                            {
                                _logger.LogWarning(readEx, "Failed to read request body for antiforgery token extraction");
                                // Even if body read fails, continue to try validation which will likely fail below
                            }
                        }

                        if (!string.IsNullOrEmpty(token))
                        {
                            // Normalize into the configured header for antiforgery validation
                            request.Headers[_options.HeaderName] = token;
                            try
                            {
                                await _antiforgery.ValidateRequestAsync(httpContext);
                                _logger.LogInformation("Anti-forgery token validated for JSON request");
                                return; // allow pipeline to continue
                            }
                            catch (AntiforgeryValidationException ex)
                            {
                                _logger.LogWarning("Anti-forgery validation failed for JSON request: {Message}", ex.Message);
                                context.Result = new JsonResult(new { success = false, message = "Invalid or expired anti-forgery token." }) { StatusCode = StatusCodes.Status403Forbidden };
                                return;
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Missing anti-forgery token in headers or JSON body for JSON request");
                            context.Result = new JsonResult(new { success = false, message = "Missing anti-forgery token. Please refresh and try again." }) { StatusCode = StatusCodes.Status403Forbidden };
                            return;
                        }
                    }
                    else
                    {
                        // Fallback to standard validation for non-JSON requests (e.g., multipart/form-data)
                        try
                        {
                            // Basic diagnostics: check cookie and header presence
                            var cookieKey = httpContext.Request.Cookies.Keys.FirstOrDefault(k => k.StartsWith(".AspNetCore.Antiforgery", StringComparison.OrdinalIgnoreCase));
                            bool hasCookie = !string.IsNullOrEmpty(cookieKey);
                            string? headerToken = request.Headers["RequestVerificationToken"].FirstOrDefault();
                            if (string.IsNullOrEmpty(headerToken)) headerToken = request.Headers["X-CSRF-TOKEN"].FirstOrDefault();
                            if (string.IsNullOrEmpty(headerToken)) headerToken = request.Headers["CSRF-TOKEN"].FirstOrDefault();

                            // If header is missing, try to read from form and normalize into header
                            if (string.IsNullOrEmpty(headerToken) && request.HasFormContentType)
                            {
                                try
                                {
                                    var form = await request.ReadFormAsync();
                                    string? formToken = form["__RequestVerificationToken"].FirstOrDefault();
                                    if (!string.IsNullOrEmpty(formToken))
                                    {
                                        request.Headers["RequestVerificationToken"] = formToken;
                                        headerToken = formToken;
                                    }
                                }
                                catch (Exception formEx)
                                {
                                    _logger.LogDebug(formEx, "Failed to read form while extracting anti-forgery token");
                                }
                            }

                            // Normalize any discovered token into the configured header name used by antiforgery options
                            if (!string.IsNullOrEmpty(headerToken))
                            {
                                request.Headers[_options.HeaderName] = headerToken;
                            }

                            _logger.LogDebug("Antiforgery diagnostics (non-JSON): hasCookie={HasCookie}, hasHeader={HasHeader}", hasCookie, !string.IsNullOrEmpty(headerToken));

                            await _antiforgery.ValidateRequestAsync(httpContext);
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
