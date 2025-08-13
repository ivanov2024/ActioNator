using System;
using System.Threading;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.Cloud
{
    /// <summary>
    /// Centralized provider for resolving a Dropbox access token according to environment and configuration.
    /// - In Development: prefers a single shared access token if configured.
    /// - In Production: uses shared access token when provided; otherwise uses per-user refresh tokens.
    /// </summary>
    public interface IDropboxTokenProvider
    {
        /// <summary>
        /// Resolve a usable Dropbox access token for the given user.
        /// </summary>
        /// <param name="userId">The user's ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A TokenResult describing how the token was obtained.</returns>
        Task<TokenResult> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public sealed class TokenResult
    {
        /// <summary>
        /// True when an access token is available.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Access token to use with Dropbox API.
        /// </summary>
        public string? AccessToken { get; init; }

        /// <summary>
        /// True when a shared access token was used instead of per-user token.
        /// </summary>
        public bool UsedSharedToken { get; init; }

        /// <summary>
        /// True when user consent (OAuth) is required to obtain a per-user token.
        /// </summary>
        public bool RequiresUserConsent { get; init; }

        /// <summary>
        /// Optional error message when Success is false and consent is not the reason.
        /// </summary>
        public string? Error { get; init; }
    }
}
