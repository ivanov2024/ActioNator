using ActioNator.Controllers;
using ActioNator.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActioNator.Areas.Coach.Controllers
{
    [Authorize]
    [Area("Coach")]
    public class HomeController : BaseController
    {
        public HomeController(UserManager<ApplicationUser> userManager) 
            : base(userManager)
        {
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Redirect Coach landing to the shared User area Home/Index
            return RedirectToAction("Index", "Home", new { area = "User" });
        }
    }
}
