using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecipeSharingPlatform.Web.Controllers;

namespace ActioNator.Areas.Coach.Controllers
{
    [Authorize]
    [Area("Coach")]
    public class HomeController : BaseController
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
