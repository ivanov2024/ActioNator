using System;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Services.Interfaces.Cloud;
using Dropbox.Api;
using Dropbox.Api.Common;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ActioNator.Services.Implementations.Cloud
{
    public class DropboxOAuthService : IDropboxOAuthService
    {
        private static readonly Uri TokenEndpoint = new("https://api.dropbox.com/oauth2/token");
        public IDropboxOAuthService.PkceCodes GeneratePkceCodes()
        {
            var verifier = DropboxOAuth2Helper.GeneratePKCECodeVerifier();
            var challenge = DropboxOAuth2Helper.GeneratePKCECodeChallenge(verifier);
            return new IDropboxOAuthService.PkceCodes
            {
                CodeVerifier = verifier,
                CodeChallenge = challenge
            };
        }

        public Uri BuildAuthorizeUri(string appKey, string redirectUri, string state, string codeChallenge)
        {
            // Required scopes for uploading files and creating/listing shared links
            var scopes = new List<string>
            {
                "files.content.write",
                "sharing.read",
                "sharing.write"
            };
            var baseUri = "https://www.dropbox.com/oauth2/authorize";
            var query = new List<string>
            {
                $"response_type=code",
                $"client_id={Uri.EscapeDataString(appKey)}",
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}",
                $"state={Uri.EscapeDataString(state)}",
                $"token_access_type=offline",
                $"code_challenge={Uri.EscapeDataString(codeChallenge)}",
                $"code_challenge_method=S256"
            };
            if (scopes?.Any() == true)
            {
                var scopeParam = string.Join(" ", scopes);
                query.Add($"scope={Uri.EscapeDataString(scopeParam)}");
            }
            var full = baseUri + "?" + string.Join("&", query);
            return new Uri(full);
        }

        public async Task<OAuth2Response> ExchangeCodeForTokenAsync(
            string code,
            string appKey,
            string redirectUri,
            string codeVerifier,
            CancellationToken cancellationToken = default)
        {
            // PKCE code flow exchange (no app secret on public client)
            var result = await DropboxOAuth2Helper.ProcessCodeFlowAsync(
                appKey,
                redirectUri,
                code,
                codeVerifier);

            return result;
        }

        public async Task<IDropboxOAuthService.RefreshResult> RefreshAccessTokenAsync(
            string appKey,
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(appKey)) throw new ArgumentException("appKey is required", nameof(appKey));
            if (string.IsNullOrWhiteSpace(refreshToken)) throw new ArgumentException("refreshToken is required", nameof(refreshToken));

            using var http = new HttpClient();
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", appKey)
            });

            using var resp = await http.PostAsync(TokenEndpoint, content, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Dropbox token refresh failed: {(int)resp.StatusCode} {resp.ReasonPhrase} - {body}");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var access = root.GetProperty("access_token").GetString() ?? string.Empty;
            string? newRefresh = null;
            if (root.TryGetProperty("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String)
            {
                newRefresh = rt.GetString();
            }
            return new IDropboxOAuthService.RefreshResult
            {
                AccessToken = access,
                RefreshToken = newRefresh
            };
        }
    }
}
