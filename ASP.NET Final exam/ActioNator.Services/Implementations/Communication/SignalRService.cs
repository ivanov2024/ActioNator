using ActioNator.Hubs;
using ActioNator.Services.Interfaces.Communication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;

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

        /// <summary>
        /// Sends a notification to all connected clients using the specified SignalR method.
        /// </summary>
        /// <param name="method">The name of the SignalR method to invoke on clients.</param>
        /// <param name="args">The arguments to pass to the SignalR method.</param>
        public async Task SendToAllAsync(string method, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Method name cannot be null or empty.", nameof(method));

            if (_hubContext == null)
            {
                _logger.LogWarning("SignalR hub context is null. Skipping notification to all clients. Method: {Method}", method);
            }

            try
            {
                await 
                    _hubContext!
                    .Clients
                    .All
                    .SendAsync(method, args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification to all clients. Method: {Method}", method);
            }
        }

        /// <summary>
        /// Sends a notification to a specific group of clients using the specified SignalR method.
        /// </summary>
        /// <param name="groupName">The name of the SignalR group to send the notification to.</param>
        /// <param name="method">The name of the SignalR method to invoke on the group clients.</param>
        /// <param name="args">The arguments to pass to the SignalR method.</param>
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


        /// <summary>
        /// Sends a notification to a specific user using the specified SignalR method.
        /// </summary>
        /// <param name="userId">The user identifier to whom the notification will be sent.</param>
        /// <param name="method">The name of the SignalR method to invoke on the user client.</param>
        /// <param name="args">The arguments to pass to the SignalR method.</param>
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
