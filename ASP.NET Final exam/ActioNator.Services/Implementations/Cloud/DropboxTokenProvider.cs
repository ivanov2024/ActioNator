using System;
using System.Threading;
using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.Services.Configuration;
using ActioNator.Services.Interfaces.Cloud;
using ActioNator.Services.Interfaces.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace ActioNator.Services.Implementations.Cloud
{
    /// <summary>
    /// Central place to decide whether to use a shared Dropbox access token or a per-user token.
    /// </summary>
    public sealed class DropboxTokenProvider : IDropboxTokenProvider
    {
        private readonly IWebHostEnvironment _env;
        private readonly IOptionsSnapshot<DropboxOptions> _options;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITokenProtector _protector;
        private readonly IDropboxOAuthService _oauth;
        private readonly ILogger<DropboxTokenProvider> _logger;

        public DropboxTokenProvider(
            IWebHostEnvironment env,
            IOptionsSnapshot<DropboxOptions> options,
            UserManager<ApplicationUser> userManager,
            ITokenProtector protector,
            IDropboxOAuthService oauth,
            ILogger<DropboxTokenProvider> logger)
        {
            _env = env;
            _options = options;
            _userManager = userManager;
            _protector = protector;
            _oauth = oauth;
            _logger = logger;
        }

        public async Task<TokenResult> GetAccessTokenAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var sharedAccess = _options.Value.SharedAccessToken?.Trim();
            var sharedRefresh = _options.Value.SharedRefreshToken?.Trim();
            var appKey = _options.Value.AppKey?.Trim();

            // 1) Shared refresh token mode (preferred when configured)
            if (!string.IsNullOrEmpty(sharedRefresh))
            {
                if (string.IsNullOrWhiteSpace(appKey))
                {
                    _logger.LogError("Dropbox AppKey is not configured; cannot refresh shared token");
                    return new TokenResult { Success = false, Error = "Dropbox AppKey is not configured" };
                }
                try
                {
                    var refreshed = await _oauth.RefreshAccessTokenAsync(appKey!, sharedRefresh, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken) && !string.Equals(refreshed.RefreshToken, sharedRefresh, StringComparison.Ordinal))
                    {
                        // We cannot persist options at runtime; instruct ops to rotate secret
                        _logger.LogWarning("Dropbox shared refresh token rotated by Dropbox. Please update configuration (user-secrets/env) with the new value.");
                    }
                    return new TokenResult { Success = true, AccessToken = refreshed.AccessToken, UsedSharedToken = true };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh shared Dropbox access token");
                    return new TokenResult { Success = false, Error = "Failed to obtain Dropbox access token" };
                }
            }

            // 2) Legacy shared access token mode
            if (!string.IsNullOrEmpty(sharedAccess))
            {
                return new TokenResult { Success = true, AccessToken = sharedAccess, UsedSharedToken = true };
            }

            // 3) Per-user flow
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new TokenResult { Success = false, Error = "User not found" };
            }

            if (string.IsNullOrWhiteSpace(user.DropboxRefreshTokenEncrypted))
            {
                return new TokenResult { Success = false, RequiresUserConsent = true };
            }

            if (string.IsNullOrWhiteSpace(appKey))
            {
                _logger.LogError("Dropbox AppKey is not configured; cannot refresh per-user token for {UserId}", userId);
                return new TokenResult { Success = false, Error = "Dropbox AppKey is not configured" };
            }

            try
            {
                var refreshToken = _protector.Unprotect(user.DropboxRefreshTokenEncrypted);
                var refreshed = await _oauth.RefreshAccessTokenAsync(appKey!, refreshToken, cancellationToken);

                // Rotate refresh token if provided
                if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
                {
                    user.DropboxRefreshTokenEncrypted = _protector.Protect(refreshed.RefreshToken);
                    var update = await _userManager.UpdateAsync(user);
                    if (!update.Succeeded)
                    {
                        _logger.LogWarning("Failed to persist new Dropbox refresh token for {UserId}: {Errors}", userId, string.Join(", ", update.Errors));
                    }
                }

                return new TokenResult { Success = true, AccessToken = refreshed.AccessToken, UsedSharedToken = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh Dropbox access token for {UserId}", userId);
                return new TokenResult { Success = false, Error = "Failed to obtain Dropbox access token" };
            }
        }
    }
}
