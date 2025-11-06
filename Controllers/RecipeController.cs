using Microsoft.AspNetCore.Mvc;

namespace IntelliMix_Core.Controllers
{
    public class RecipeController : Controller
    {
        // View for Recipe Index
        public IActionResult Index()
        {
            return View();
        }
    }
}
