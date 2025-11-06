using Intellimix_Core.Models;
using IntelliMix_Core.Models;
using IntelliMix_Core.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace IntelliMix_Core.Controllers
{
    public class MaterialTypeController : Controller
    {
        private readonly ITokenService _tokenService;

        public MaterialTypeController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        // ================================
        //  MAIN PAGE - MATERIAL TYPE MASTER
        // ================================
        public async Task<IActionResult> Index(string filter = "all")
        {
            ViewBag.ApiStatus = HttpContext.Session.GetString("ApiStatus");

            var materials = await GetMaterialTypesAsync() ?? new List<MaterialTypeModel>();

            ViewBag.Materials = materials;
            ViewBag.CurrentFilter = filter;
            ViewBag.TotalTypes = materials.Count;
            ViewBag.ActiveTypes = materials.Count(m => m.active_flag ?? false);

            ViewBag.InactiveTypes = materials.Count(m => !(m.active_flag ?? false));
            ViewBag.TotalMaterials = 45;

            return View(materials);
        }

        public async Task<IActionResult> View(string filter = "all")
        {
            ViewBag.ApiStatus = HttpContext.Session.GetString("ApiStatus");

            var materials = await GetMaterialTypesAsync() ?? new List<MaterialTypeModel>();

            ViewBag.Materials = materials;
            ViewBag.CurrentFilter = filter;
            ViewBag.TotalTypes = materials.Count;
            ViewBag.ActiveTypes = materials.Count(m => m.active_flag ?? false);
            ViewBag.InactiveTypes = materials.Count(m => !(m.active_flag ?? false));

            ViewBag.TotalMaterials = 45;

            return View(materials);
        }

        // ================================
        //  FETCH MATERIAL TYPES (GET)
        // ================================
        private async Task<List<MaterialTypeModel>> GetMaterialTypesAsync()
        {
            //string token = await GetAccessTokenAsync();
            string token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return new List<MaterialTypeModel>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("http://localhost:5254/api/v1/MaterialType");
                if (!response.IsSuccessStatusCode)
                    return new List<MaterialTypeModel>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<MaterialTypeModel>>(json)
                       ?? new List<MaterialTypeModel>();
            }
        }

        // ================================
        //  SAVE MATERIAL (POST)
        // ================================
        [HttpPost]
        public async Task<IActionResult> SaveMaterial(
            string material_type_name,
            string material_type_code,
            string description,
            string default_storage_condition,
            bool active_flag)
        {
            bool success = false;
            string message = "";

            try
            {
                //var token = await GetAccessTokenAsync();
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    message = "⚠️ Session expired. Please log in again.";
                    return Json(new { success, message });
                }

                var payload = new
                {
                    material_type_id = Guid.NewGuid(),
                    material_type_name,
                    material_type_code,
                    description,
                    hazardous_flag = true,
                    default_storage_condition = default_storage_condition ?? "Standard",
                    active_flag,
                    deleted_flag = false


                };

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://localhost:5254/api/v1/MaterialType", content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        message = "✅ Material Type inserted successfully!";
                        success = true;
                    }
                    else
                    {
                        var errorObj = JObject.Parse(result);
                        var detailMsg = errorObj["detail"]?.ToString() ?? result;
                        message = $"Insert error: {detailMsg}";
                    }
                }
            }
            catch (Exception ex)
            {
                message = "Insert error: Exception occurred. " + ex.Message;
            }

            return Json(new { success, message });
        }
        // ================================
        //  EXPORT CSV 
        // ================================
        public async Task<ActionResult> ExportCsv(string filter = "all")
        {
            // Get the list of materials (strongly typed)
            var materials = await GetMaterialTypesAsync(); // returns List<MaterialTypeModel>

            // Apply filter
            if (filter == "active")
                materials = materials.Where(m => m.active_flag ?? false).ToList();
            else if (filter == "inactive")
                materials = materials.Where(m => !(m.active_flag ?? false)).ToList();

            // Build CSV
            var csv = new StringBuilder();
            csv.AppendLine("Type Code,Type Name,Description,Default Storage Condition,Status,Created At,Updated At");

            foreach (var item in materials)
            {
                string status = (item.active_flag ?? false) ? "Active" : "Inactive";
                string createdAt = item.created_at?.ToString("yyyy-MM-dd HH:mm") ?? "";
                string updatedAt = item.updated_at?.ToString("yyyy-MM-dd HH:mm") ?? "";

                // Escape commas in fields by wrapping in quotes
                csv.AppendLine($"\"{item.material_type_code}\",\"{item.material_type_name}\",\"{item.description}\",\"{item.default_storage_condition}\",\"{status}\",\"{createdAt}\",\"{updatedAt}\"");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", "MaterialTypes.csv");
        }

       
        // ================================
        //  DELETE MATERIAL TYPE (POST)
        // ================================
        [HttpPost]
        public async Task<ActionResult> DeleteMaterial(Guid id)
        {
            bool success = false;
            string message = "";

            if (id == Guid.Empty)
                return Json(new { success, message = "Invalid Material Type ID" });

            try
            {
                //var token = await GetAccessTokenAsync();
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    message = "⚠️ Session expired. Please log in again.";
                    return Json(new { success, message });
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var apiUrl = $"http://localhost:5254/api/v1/MaterialType/{id}";

                    // Send POST request to delete
                    var payload = new { id = id };
                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        success = true;
                        message = "Material Type deleted successfully"; // ✅ always show this
                    }
                    else
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        message = $"❌ API Error: {result}";
                    }
                }
            }
            catch (Exception ex)
            {
                message = "❌ Exception occurred while deleting: " + ex.Message;
            }

            return Json(new { success, message });
        }


        // ================================
        //  UPDATE MATERIAL TYPE (POST)
        // ================================
        [HttpPost]
        public async Task<IActionResult> UpdateMaterial(Guid id, string material_type_name, string material_type_code, string description, string default_storage_condition, bool active_flag)
        {
            bool success = false;
            string message = "";

            if (id == Guid.Empty)
                return Json(new { success, message = "❌ Invalid Material Type ID" });

            try
            {
                //var token = await GetAccessTokenAsync();
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    message = "⚠️ Session expired. Please log in again.";
                    return Json(new { success, message });
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var apiUrl = $"http://localhost:5254/api/v1/MaterialType/{id}";

                    var payload = new
                    {
                        material_type_name,
                        material_type_code,
                        description,
                        default_storage_condition,
                        active_flag,
                        deleted_flag = false
                    };

                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PutAsync(apiUrl, content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        success = true;
                        message = "Material Type updated successfully";
                    }
                    else
                    {
                        message = $"❌ API Error: {result}";
                    }
                }
            }
            catch (Exception ex)
            {
                message = "❌ Exception occurred while updating: " + ex.Message;
            }

            return Json(new { success, message });
        }

        // ================================
        //  GET TOKEN (REUSED)
        // ================================
        public async Task<string> GetAccessTokenAsync1()
        {
            var token = HttpContext.Session.GetString("AuthToken");
            if (!string.IsNullOrEmpty(token))
                return token;

            var username = HttpContext.Session.GetString("User");
            var password = HttpContext.Session.GetString("Password");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

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

                    HttpContext.Session.SetString("AuthToken", tokenResponse.AccessToken);
                    return tokenResponse.AccessToken;
                }
            }
            catch
            {
                return null;
            }
        }
        // ================================
        //  IMPORT CSV
        // ================================


        [HttpPost]
        public async Task<IActionResult> ImportCsv()
        {
            bool success = false;
            string message = "";
            int insertedCount = 0, failedCount = 0;

            try
            {
                var files = Request.Form.Files;
                if (files.Count == 0 || files[0].Length == 0)
                    return Json(new { success, message = "❌ No file uploaded or file is empty." });

                var file = files[0];
                //string token = await GetAccessTokenAsync();
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return Json(new { success, message = "⚠️ Session expired. Please log in again." });

                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    string headerLine = await reader.ReadLineAsync(); // skip header
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var cols = line.Split(',');
                        if (cols.Length < 4) // adjust according to your CSV columns
                        {
                            failedCount++;
                            continue;
                        }

                        var payload = new
                        {
                            material_type_id = Guid.NewGuid(),
                            material_type_code = cols[0].Trim(),
                            material_type_name = cols[1].Trim(),
                            description = cols[2].Trim(),
                            default_storage_condition = cols[3].Trim(),
                            hazardous_flag = true,
                            active_flag = true,
                            deleted_flag = false
                        };

                        var json = JsonConvert.SerializeObject(payload);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("http://localhost:5254/api/v1/MaterialType", content);

                        if (response.IsSuccessStatusCode)
                            insertedCount++;
                        else
                            failedCount++;
                    }
                }

                success = true;
                message = $"✅ Import completed. Inserted: {insertedCount}, Failed: {failedCount}.";
            }
            catch (Exception ex)
            {
                message = "❌ Error during import: " + ex.Message;
            }

            return Json(new { success, message });
        }

    }


}
