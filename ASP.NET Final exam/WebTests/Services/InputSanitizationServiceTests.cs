using System;
using System.Collections.Generic;
using ActioNator.Services.Implementations.InputSanitization;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace WebTests.Services
{
    public class InputSanitizationServiceTests
    {
        private static InputSanitizationService CreateService()
        {
            var logger = new Mock<ILogger<InputSanitizationService>>(MockBehavior.Loose);
            return new InputSanitizationService(logger.Object);
        }

        [Test]
        public void Constructor_NullLogger_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new InputSanitizationService(null!));
        }

        [Test]
        public void SanitizeString_RemovesScripts_AndHtmlEncodes()
        {
            var service = CreateService();
            var input = "<div>Hello<script>alert(1)</script></div>";

            var result = service.SanitizeString(input);

            Assert.That(result, Does.Not.Contain("<script"));
            Assert.That(result, Does.Contain("&lt;div&gt;Hello"));
            Assert.That(result, Does.Contain("&lt;/div&gt;"));
        }

        [Test]
        public void SanitizeString_NullOrEmpty_ReturnsSame()
        {
            var service = CreateService();
            string? nullInput = null;
            var empty = string.Empty;

            var r1 = service.SanitizeString(nullInput!);
            var r2 = service.SanitizeString(empty);

            Assert.That(r1, Is.Null);
            Assert.That(r2, Is.EqualTo(string.Empty));
        }

        [Test]
        public void SanitizeString_Rewrites_OnEvent_And_Encodes()
        {
            var service = CreateService();
            var input = "<a onclick=\"do()\">x</a>";

            var result = service.SanitizeString(input);

            // Ensure the original attribute is removed; allow the rewritten marker
            Assert.That(result, Does.Not.Contain(" onclick"));
            Assert.That(result, Does.Contain("data-removed-onclick"));
            Assert.That(result, Does.Contain("&lt;a"));
        }

        [Test]
        public void SanitizeString_Replaces_JavaScript_Protocol()
        {
            var service = CreateService();
            var input = "<a href=\"javascript:alert(1)\">x</a>";

            var result = service.SanitizeString(input);

            // javascript: should be replaced with invalid:
            Assert.That(result, Does.Contain("invalid:"));
            Assert.That(result, Does.Not.Contain("javascript:"));
        }

        [Test]
        public void SanitizeHtml_Removes_Scripts_And_OnEvent_Allows_SafeTags()
        {
            var service = CreateService();
            var input = "<div onclick=\"x\"><script>bad()</script><b>ok</b></div>";

            var result = service.SanitizeHtml(input);

            Assert.That(result, Does.Not.Contain("<script"));
            Assert.That(result, Does.Not.Contain("onclick"));
            Assert.That(result, Does.Contain("<b>ok</b>"));
        }

        [Test]
        public void SanitizeHtml_NullOrEmpty_ReturnsSame()
        {
            var service = CreateService();
            string? nullInput = null;
            var empty = string.Empty;

            var r1 = service.SanitizeHtml(nullInput!);
            var r2 = service.SanitizeHtml(empty);

            Assert.That(r1, Is.Null);
            Assert.That(r2, Is.EqualTo(string.Empty));
        }

        [Test]
        public void IsValidInput_Empty_IsTrue()
        {
            var service = CreateService();
            Assert.That(service.IsValidInput(string.Empty), Is.True);
        }

        [Test]
        public void IsValidInput_Null_IsTrue()
        {
            var service = CreateService();
            Assert.That(service.IsValidInput(null!), Is.True);
        }

        [Test]
        public void IsValidInput_Detects_Malicious_Patterns()
        {
            var service = CreateService();
            Assert.That(service.IsValidInput("<script>alert(1)</script>"), Is.False);
            Assert.That(service.IsValidInput("onclick=\"x\""), Is.False);
            Assert.That(service.IsValidInput("javascript:alert(1)"), Is.False);
            Assert.That(service.IsValidInput("select * from users"), Is.False);
        }

        [Test]
        public void TryValidateInput_Returns_Detailed_Errors()
        {
            var service = CreateService();
            var input = "javascript:alert(1)<script>bad</script> onclick=\"x\" SELECT * FROM users";

            var ok = service.TryValidateInput(input, out var errors);

            Assert.That(ok, Is.False);
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors, Has.Some.Contains("script tags"));
            Assert.That(errors, Has.Some.Contains("event handlers"));
            Assert.That(errors, Has.Some.Contains("JavaScript URLs"));
            Assert.That(errors, Has.Some.Contains("SQL injection"));
        }

        [Test]
        public void TryValidateInput_NullOrEmpty_ReturnsTrue_And_NoErrors()
        {
            var service = CreateService();

            var okNull = service.TryValidateInput(null!, out var errorsNull);
            var okEmpty = service.TryValidateInput(string.Empty, out var errorsEmpty);

            Assert.That(okNull, Is.True);
            Assert.That(errorsNull, Is.Empty);
            Assert.That(okEmpty, Is.True);
            Assert.That(errorsEmpty, Is.Empty);
        }

        [Test]
        public void SanitizeDictionary_Sanitizes_Keys_And_Values()
        {
            var service = CreateService();
            var dict = new Dictionary<string, string>
            {
                ["<key>"] = "<value>"
            };

            var sanitized = service.SanitizeDictionary(dict);

            Assert.That(sanitized.ContainsKey("&lt;key&gt;"), Is.True);
            Assert.That(sanitized["&lt;key&gt;"], Is.EqualTo("&lt;value&gt;"));
        }

        [Test]
        public void SanitizeDictionary_Null_ReturnsEmpty()
        {
            var service = CreateService();
            var sanitized = service.SanitizeDictionary(null!);
            Assert.That(sanitized, Is.Empty);
        }

        [Test]
        public void HtmlEncode_Encodes_Special_Characters()
        {
            var service = CreateService();
            var result = service.HtmlEncode("<>&\"");

            Assert.That(result, Does.Contain("&lt;"));
            Assert.That(result, Does.Contain("&gt;"));
            Assert.That(result, Does.Contain("&amp;"));
            Assert.That(result, Does.Contain("&quot;"));
        }

        [Test]
        public void HtmlEncode_NullOrEmpty_ReturnsSame()
        {
            var service = CreateService();
            string? nullInput = null;
            var empty = string.Empty;

            var r1 = service.HtmlEncode(nullInput!);
            var r2 = service.HtmlEncode(empty);

            Assert.That(r1, Is.Null);
            Assert.That(r2, Is.EqualTo(string.Empty));
        }

        [Test]
        public void JavaScriptEncode_Escapes_Properly()
        {
            var service = CreateService();
            var input = "a'b\"<>\n\t\b\f\r\\";

            var encoded = service.JavaScriptEncode(input);

            Assert.That(encoded, Does.Contain("\\'"));
            Assert.That(encoded, Does.Contain("\\\""));
            Assert.That(encoded, Does.Contain("\\u003C"));
            Assert.That(encoded, Does.Contain("\\u003E"));
            Assert.That(encoded, Does.Contain("\\n"));
            Assert.That(encoded, Does.Contain("\\t"));
            Assert.That(encoded, Does.Contain("\\b"));
            Assert.That(encoded, Does.Contain("\\f"));
            Assert.That(encoded, Does.Contain("\\r"));
            Assert.That(encoded, Does.Contain("\\\\"));
        }

        [Test]
        public void JavaScriptEncode_NullOrEmpty_ReturnsSame()
        {
            var service = CreateService();
            string? nullInput = null;
            var empty = string.Empty;

            var r1 = service.JavaScriptEncode(nullInput!);
            var r2 = service.JavaScriptEncode(empty);

            Assert.That(r1, Is.Null);
            Assert.That(r2, Is.EqualTo(string.Empty));
        }
    }
}
