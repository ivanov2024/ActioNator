using System;
using System.Threading.Tasks;

namespace ActioNator.Services.Interfaces.Communication
{
    /// <summary>
    /// Interface for SignalR communication service
    /// </summary>
    public interface ISignalRService
    {
        /// <summary>
        /// Sends a notification to all clients
        /// </summary>
        /// <param name="method">SignalR method name</param>
        /// <param name="args">Arguments to pass to the method</param>
        Task SendToAllAsync(string method, params object[] args);

        /// <summary>
        /// Sends a notification to a specific group
        /// </summary>
        /// <param name="groupName">Group name</param>
        /// <param name="method">SignalR method name</param>
        /// <param name="args">Arguments to pass to the method</param>
        Task SendToGroupAsync(string groupName, string method, params object[] args);

        /// <summary>
        /// Sends a notification to a specific user
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="method">SignalR method name</param>
        /// <param name="args">Arguments to pass to the method</param>
        Task SendToUserAsync(string userId, string method, params object[] args);
    }
}
