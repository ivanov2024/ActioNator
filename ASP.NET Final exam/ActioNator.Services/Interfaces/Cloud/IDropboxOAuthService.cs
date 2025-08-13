using System;
using System.Threading;
using System.Threading.Tasks;
using Dropbox.Api;

namespace ActioNator.Services.Interfaces.Cloud
{
    public interface IDropboxOAuthService
    {
        public sealed class PkceCodes
        {
            public string CodeVerifier { get; init; } = string.Empty;
            public string CodeChallenge { get; init; } = string.Empty;
        }

        public sealed class RefreshResult
        {
            public string AccessToken { get; init; } = string.Empty;
            public string? RefreshToken { get; init; }
        }

        PkceCodes GeneratePkceCodes();

        Uri BuildAuthorizeUri(
            string appKey,
            string redirectUri,
            string state,
            string codeChallenge);

        Task<OAuth2Response> ExchangeCodeForTokenAsync(
            string code,
            string appKey,
            string redirectUri,
            string codeVerifier,
            CancellationToken cancellationToken = default);

        Task<RefreshResult> RefreshAccessTokenAsync(
            string appKey,
            string refreshToken,
            CancellationToken cancellationToken = default);
    }
}
