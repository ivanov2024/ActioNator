using ActioNator.Controllers;
using ActioNator.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ActioNator.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class HomeController : BaseController
    {
        public HomeController(UserManager<ApplicationUser> userManager) 
            : base(userManager)
        {
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
