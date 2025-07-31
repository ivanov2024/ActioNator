using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeSharingPlatform.Web.Controllers;

namespace ActioNator.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class HomeController : BaseController
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
