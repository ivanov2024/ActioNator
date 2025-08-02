using ActioNator.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActioNator.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseController(UserManager<ApplicationUser> userManager)
            => _userManager = userManager 
            ?? throw new ArgumentNullException(nameof(userManager));

        protected bool IsUserAuthenticated()
            => User.Identity?.IsAuthenticated ?? false;

        protected Guid? GetUserId()
        {
            if (!IsUserAuthenticated())
                return null;

            string? userIdString 
                = User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdString, out Guid userId))
                return userId;

            return null;
        }

        protected async Task<ApplicationUser> GetUserAsync(Guid userId)
        {
            ApplicationUser? user 
                = await _userManager
                .FindByIdAsync(userId.ToString());

            return user 
                ?? throw new InvalidOperationException("User not found.");
        }
    }
}
