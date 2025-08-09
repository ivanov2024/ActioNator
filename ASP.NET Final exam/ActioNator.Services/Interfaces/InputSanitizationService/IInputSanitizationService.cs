namespace ActioNator.Services.Interfaces.InputSanitizationService
{
    /// <summary>
    /// Interface for sanitizing and validating user input to protect against malicious inputs
    /// such as injection attacks, XSS, and other common input threats.
    /// </summary>
    public interface IInputSanitizationService
    {
        /// <summary>
        /// Sanitizes a string input to prevent XSS and other injection attacks
        /// </summary>
        /// <param name="input">The raw input string to sanitize</param>
        /// <returns>A sanitized version of the input string</returns>
        string SanitizeString(string input);

        /// <summary>
        /// Sanitizes HTML content to remove potentially malicious scripts and markup
        /// </summary>
        /// <param name="htmlContent">The raw HTML content to sanitize</param>
        /// <returns>A sanitized version of the HTML content</returns>
        string SanitizeHtml(string htmlContent);

        /// <summary>
        /// Validates if a string input meets security requirements
        /// </summary>
        /// <param name="input">The input string to validate</param>
        /// <returns>True if the input is valid, false otherwise</returns>
        bool IsValidInput(string input);

        /// <summary>
        /// Validates if a string input meets security requirements and returns validation errors
        /// </summary>
        /// <param name="input">The input string to validate</param>
        /// <param name="validationErrors">List of validation errors if any</param>
        /// <returns>True if the input is valid, false otherwise</returns>
        bool TryValidateInput(string input, out IList<string> validationErrors);

        /// <summary>
        /// Sanitizes a dictionary of key-value pairs
        /// </summary>
        /// <param name="inputDictionary">Dictionary containing input values to sanitize</param>
        /// <returns>A new dictionary with sanitized values</returns>
        IDictionary<string, string> SanitizeDictionary(IDictionary<string, string> inputDictionary);

        /// <summary>
        /// Encodes a string for safe output in HTML context
        /// </summary>
        /// <param name="input">The input string to encode</param>
        /// <returns>HTML encoded string</returns>
        string HtmlEncode(string input);

        /// <summary>
        /// Encodes a string for safe output in JavaScript context
        /// </summary>
        /// <param name="input">The input string to encode</param>
        /// <returns>JavaScript encoded string</returns>
        string JavaScriptEncode(string input);
    }
}
