using System.Threading.Tasks;
using ActioNator.Data.Models;
using ActioNator.ViewModels.Dashboard;

namespace ActioNator.Services.Interfaces.UserDashboard
{
    public interface IUserDashboardService
    {
        /// <summary>
        /// Gets dashboard data for a specific user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="user">The user</param>
        /// <returns>Dashboard view model with user-specific data</returns>
        Task<DashboardViewModel> GetDashboardDataAsync(Guid userId, ApplicationUser user);
    }
}
