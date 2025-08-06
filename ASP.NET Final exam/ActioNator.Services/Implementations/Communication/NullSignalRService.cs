using ActioNator.Services.Interfaces.Communication;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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

        /// <inheritdoc />
        public Task SendToAllAsync(string method, params object[] args)
        {
            _logger.LogDebug("NullSignalRService: SendToAllAsync called with method {Method}", method);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SendToGroupAsync(string groupName, string method, params object[] args)
        {
            _logger.LogDebug("NullSignalRService: SendToGroupAsync called with group {Group} and method {Method}", 
                groupName, method);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SendToUserAsync(string userId, string method, params object[] args)
        {
            _logger.LogDebug("NullSignalRService: SendToUserAsync called with user {UserId} and method {Method}", 
                userId, method);
            return Task.CompletedTask;
        }
    }
}
