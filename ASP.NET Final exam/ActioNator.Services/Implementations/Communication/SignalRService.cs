using ActioNator.Hubs;
using ActioNator.Services.Interfaces.Communication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ActioNator.Services.Implementations.Communication
{
    /// <summary>
    /// Implementation of SignalR communication service
    /// </summary>
    public class SignalRService : ISignalRService
    {
        private readonly IHubContext<CommunityHub>? _hubContext;
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(IHubContext<CommunityHub>? hubContext, ILogger<SignalRService> logger)
        {
            _hubContext = hubContext; // Allow null for design-time operations
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task SendToAllAsync(string method, params object[] args)
        {
            try
            {
                if (_hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync(method, args);
                }
                else
                {
                    _logger.LogWarning("SignalR hub context is null. Skipping notification to all clients. Method: {Method}", method);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification to all clients. Method: {Method}", method);
            }
        }

        /// <inheritdoc />
        public async Task SendToGroupAsync(string groupName, string method, params object[] args)
        {
            try
            {
                if (_hubContext != null)
                {
                    await _hubContext.Clients.Group(groupName).SendAsync(method, args);
                }
                else
                {
                    _logger.LogWarning("SignalR hub context is null. Skipping notification to group {Group}. Method: {Method}", 
                        groupName, method);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification to group {Group}. Method: {Method}", 
                    groupName, method);
            }
        }

        /// <inheritdoc />
        public async Task SendToUserAsync(string userId, string method, params object[] args)
        {
            try
            {
                if (_hubContext != null)
                {
                    await _hubContext.Clients.User(userId).SendAsync(method, args);
                }
                else
                {
                    _logger.LogWarning("SignalR hub context is null. Skipping notification to user {UserId}. Method: {Method}", 
                        userId, method);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification to user {UserId}. Method: {Method}", 
                    userId, method);
            }
        }
    }
}
