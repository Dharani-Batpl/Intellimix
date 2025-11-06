using Intellimix_Core.Models;
using IntelliMix_Core.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace IntelliMix_Core.Controllers
{
    public class UserController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login1(string username, string password)
        {
            // Demo authentication
            if (username == "admin" && password == "pass")
            {
                HttpContext.Session.SetString("User", username);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid username or password.";
            return View("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ================================
        // TOKEN MANAGEMENT
        // ================================
        private async Task<string> GetAccessTokenAsync(string username, string password)
        {
            // Check if token already exists in session
            var existingToken = HttpContext.Session.GetString("AuthToken");
            if (!string.IsNullOrEmpty(existingToken))
                return existingToken;

            try
            {
                using (var client = new HttpClient())
                {
                    var body = new { username, password };
                    var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://localhost:5254/api/token/login", content);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    var json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);

                    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                        return null;

                    // ✅ Store token & user in session
                    HttpContext.Session.SetString("AuthToken", tokenResponse.AccessToken);
                    HttpContext.Session.SetString("User", username);

                    return tokenResponse.AccessToken;
                }
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, bool startup = false)
        {
            if (startup)
                return View();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View();
            }

            var token = await GetAccessTokenAsync(username, password);

            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Error = "Invalid username or password.";
                return View();
            }

            // ✅ Successful login
            HttpContext.Session.SetString("AuthToken", token);
            HttpContext.Session.SetString("User", username);

            return RedirectToAction("Index", "Home");
        }
    }

   
}
