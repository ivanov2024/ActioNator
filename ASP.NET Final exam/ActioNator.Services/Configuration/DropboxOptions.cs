using System;

namespace ActioNator.Services.Configuration
{
    public sealed class DropboxOptions
    {
        public const string SectionName = "Dropbox";
        public string? AppKey { get; set; }
        // For PKCE on confidential-less flows, AppSecret is not required on localhost
        public string? AppSecret { get; set; }
        public string? RedirectUri { get; set; } // optional, can be overridden per environment
        /// <summary>
        /// Optional: a single shared long-lived access token. In Development this will be preferred
        /// when present; in Production, if provided, it enables single-token mode instead of per-user tokens.
        /// </summary>
        public string? SharedAccessToken { get; set; }

        /// <summary>
        /// Optional: a single shared refresh token. When configured, the app will request a short-lived
        /// access token on demand via OAuth refresh and use it for Dropbox operations. If Dropbox rotates
        /// the refresh token, the application will log a warning instructing to update the configured value.
        /// </summary>
        public string? SharedRefreshToken { get; set; }
    }
}
