using Microsoft.AspNetCore.Http; // Required for session
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IntelliMix_Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemoryCache _cache;

        public HomeController(IMemoryCache cache)
        {
            _cache = cache;
        }
        public async Task<IActionResult> Index()
        {
            var apiStatus = _cache.Get<string>("ApiStatus") ?? "Unknown";
            ViewBag.ApiStatus = apiStatus;

            HttpContext.Session.SetString("ApiStatus", apiStatus);

            ViewBag.Title = "Dashboard";
            ViewBag.ActiveMenu = "Dashboard";

            return View();
        }

        public IActionResult ZoneMaster()
        {
            ViewBag.Title = "Zone Master";
            ViewBag.ActiveMenu = "Masters";
            return View();
        }

        public IActionResult MaterialType()
        {

            ViewBag.Title = "Material Type";
            ViewBag.ActiveMenu = "Masters";
            return View();
        }

        public IActionResult Inventory()
        {
            ViewBag.Title = "Inventory";
            ViewBag.ActiveMenu = "Inventory";
            return View();
        }

        // ================================
        //  API HEALTH CHECK (will be removed)
        // ================================
        public async Task<string> CheckApiHealthAsync1()
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://localhost:5254/health");

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        return result.Contains("Healthy") ? "Healthy" : "Warning";
                    }
                    else
                    {
                        return "Unhealthy";
                    }
                }
                catch (Exception)
                {
                    return "Unhealthy";
                }
            }
        }
    }
}
