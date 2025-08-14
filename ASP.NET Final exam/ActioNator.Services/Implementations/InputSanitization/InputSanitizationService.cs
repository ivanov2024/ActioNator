using ActioNator.Services.Interfaces.InputSanitizationService;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Web;
using Ganss.Xss;


namespace ActioNator.Services.Implementations.InputSanitization
{
    /// <summary>
    /// Implementation of IInputSanitizationService that provides methods for sanitizing and validating user input
    /// to protect against malicious inputs such as injection attacks, XSS, and other common input threats.
    /// </summary>
    public class InputSanitizationService : IInputSanitizationService
    {
        /// <summary>
        /// Logger instance for capturing diagnostics, exceptions, and audit trails 
        /// related to the input sanitization process.
        /// </summary>
        private readonly ILogger<InputSanitizationService> _logger;

        /// <summary>
        /// Instance of the HtmlSanitizer library responsible for robust
        /// and comprehensive sanitization of HTML content to prevent
        /// cross-site scripting (XSS) and related injection attacks.
        /// Configurable to allow or restrict specific HTML tags, attributes,
        /// and URI schemes according to application security policies.
        /// </summary>
        private readonly HtmlSanitizer _htmlSanitizer;

        /// <summary>
        /// Detects and removes potential <script> tags used for XSS attacks.
        /// This pattern matches any script block, regardless of attributes or content.
        /// </summary>
        private static readonly Regex _scriptTagPattern 
            = new (@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Identifies inline HTML event handler attributes (e.g., onclick, onload)
        /// which could be used to inject JavaScript payloads in HTML contexts.
        /// </summary>
        private static readonly Regex _onEventPattern 
            = new (@"(?:^|\s)(on\w+)\s*=\s*", RegexOptions.IgnoreCase);

        /// <summary>
        /// Matches "javascript:" protocol usage, which may be used in href, src,
        /// or other attributes to execute arbitrary JavaScript in XSS attacks.
        /// </summary>s
        private static readonly Regex _jsUrlPattern 
            = new (@"javascript:", RegexOptions.IgnoreCase);

        /// <summary>
        /// Detects patterns commonly associated with SQL Injection attempts, including
        /// key SQL commands and logical operators. This regex is intentionally aggressive
        /// and should be used alongside parameterized queries for defense-in-depth.
        /// </summary>
        private static readonly Regex _sqlInjectionPattern 
            = new (
    @"(\s*([\0\b\'\""\n\r\t%_\\]*\s*(((select\s+.+\s+from\s+.+)" +
            @"|(insert\s+.+\s+into\s+.+)" +
            @"|(update\s+.+\s+set\s+.+)" +
            @"|(delete\s+.+\s+from\s+.+)" +
            @"|(drop\s+.+)" +
            @"|(truncate\s+.+)" +
            @"|(alter\s+.+)" +
            @"|(exec\s+.+)" +
            @"|(\s*(all|any|not|and|between|in|like|or|some|contains|containsall|containskey)\s+.+[=><=!~]+.+)" +
            @"|(let\s+.+\s*=\s*.+)" +
            @"|(begin|cursor|declare|exec|execute))\s*;?)))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);


        /// <summary>
        /// Initializes a new instance of the InputSanitizationService class
        /// </summary>
        /// <param name="logger">Logger for diagnostic information</param>
        public InputSanitizationService(ILogger<InputSanitizationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _htmlSanitizer = new HtmlSanitizer();
        }

        public string SanitizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            try
            {
                // Remove potentially dangerous patterns
                string sanitized = input;
                sanitized = _scriptTagPattern.Replace(sanitized, string.Empty);
                sanitized = _onEventPattern.Replace(sanitized, " data-removed-$1=");
                sanitized = _jsUrlPattern.Replace(sanitized, "invalid:");
                
                // Encode special characters
                sanitized = HttpUtility.HtmlEncode(sanitized);
                
                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing string input");
                // Return a safe empty string in case of error
                return string.Empty;
            }
        }

        public string SanitizeHtml(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
            {
                return htmlContent;
            }

            try
            {
                // Using HtmlSanitizer to clean the HTML content robustly
                string sanitized = _htmlSanitizer
                    .Sanitize(htmlContent);

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing HTML content");
                // Return a safe empty string in case of error
                return string.Empty;
            }
        }

        public bool IsValidInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true; // Empty input is considered valid
            }

            try
            {
                // Check for potentially malicious patterns
                if (_scriptTagPattern.IsMatch(input) ||
                    _onEventPattern.IsMatch(input) ||
                    _jsUrlPattern.IsMatch(input) ||
                    _sqlInjectionPattern.IsMatch(input))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating input");
                return false; // Consider invalid in case of error
            }
        }

        public bool TryValidateInput(string input, out IList<string> validationErrors)
        {
            validationErrors = new List<string>();

            if (string.IsNullOrEmpty(input))
            {
                return true; // Empty input is considered valid
            }

            try
            {
                // Check for potentially malicious patterns
                if (_scriptTagPattern.IsMatch(input))
                {
                    validationErrors.Add("Input contains potentially malicious script tags");
                }
                
                if (_onEventPattern.IsMatch(input))
                {
                    validationErrors.Add("Input contains potentially malicious event handlers");
                }
                
                if (_jsUrlPattern.IsMatch(input))
                {
                    validationErrors.Add("Input contains potentially malicious JavaScript URLs");
                }
                
                if (_sqlInjectionPattern.IsMatch(input))
                {
                    validationErrors.Add("Input contains potential SQL injection patterns");
                }

                return validationErrors.Count == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating input with detailed errors");
                validationErrors.Add("An error occurred while validating the input");
                return false;
            }
        }

        public IDictionary<string, string> SanitizeDictionary(IDictionary<string, string> inputDictionary)
        {
            if (inputDictionary == null)
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var sanitizedDictionary = new Dictionary<string, string>();
                
                foreach (var kvp in inputDictionary)
                {
                    // Sanitize both keys and values
                    string sanitizedKey = SanitizeString(kvp.Key);
                    string sanitizedValue = SanitizeString(kvp.Value);
                    
                    sanitizedDictionary[sanitizedKey] = sanitizedValue;
                }
                
                return sanitizedDictionary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sanitizing dictionary");
                return new Dictionary<string, string>(); // Return empty dictionary in case of error
            }
        }

        public string HtmlEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            try
            {
                return HttpUtility.HtmlEncode(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error HTML encoding input");
                return string.Empty; // Return empty string in case of error
            }
        }

        public string JavaScriptEncode(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            try
            {
                // Basic JavaScript encoding
                // For a production environment, consider using a dedicated library
                // like Microsoft.Security.Application.Encoder.JavaScriptEncode
                
                string encoded = input
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t")
                    .Replace("\b", "\\b")
                    .Replace("\f", "\\f")
                    .Replace("<", "\\u003C")
                    .Replace(">", "\\u003E");
                
                return encoded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error JavaScript encoding input");
                return string.Empty; // Return empty string in case of error
            }
        }
    }
}
