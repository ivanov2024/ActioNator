using ActioNator.Services.Interfaces.Communication;
using Microsoft.Extensions.Logging;

namespace ActioNator.Services.Implementations.Communication
{
    /// <summary>
    /// Null implementation of ISignalRService for design-time operations
    /// </summary>
    public class NullSignalRService : ISignalRService
    {
        private readonly ILogger<NullSignalRService> _logger;

        public NullSignalRService(ILogger<NullSignalRService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs SignalR notification attempts without sending any notifications.
        /// Useful for design-time or testing scenarios where no actual SignalR communication is needed.
        /// </summary>
        /// <param name="method">The name of the SignalR method that would have been called.</param>
        /// <param name="args">The arguments that would have been passed to the method.</param>
        public Task SendToAllAsync(string method, params object[] args)
        {
            _logger.LogDebug("NullSignalRService: SendToAllAsync called with method {Method}", method);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs SignalR notification attempts to a specific group without sending any notifications.
        /// Useful for design-time or testing scenarios where no actual SignalR communication is needed.
        /// </summary>
        /// <param name="groupName">The name of the group that would have received the notification.</param>
        /// <param name="method">The name of the SignalR method that would have been called.</param>
        /// <param name="args">The arguments that would have been passed to the method.</param>
        public Task SendToGroupAsync(string groupName, string method, params object[] args)
        {
            _logger.LogDebug("NullSignalRService: SendToGroupAsync called with group {Group} and method {Method}", 
                groupName, method);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs SignalR notification attempts to a specific user without sending any notifications.
        /// Useful for design-time or testing scenarios where no actual SignalR communication is needed.
        /// </summary>
        /// <param name="userId">The user ID that would have received the notification.</param>
        /// <param name="method">The name of the SignalR method that would have been called.</param>
        /// <param name="args">The arguments that would have been passed to the method.</param>
        public Task SendToUserAsync(string userId, string method, params object[] args)
        {
            _logger.LogDebug("NullSignalRService: SendToUserAsync called with user {UserId} and method {Method}", 
                userId, method);
            return Task.CompletedTask;
        }
    }
}
