using Intellimix_Core.Models;
using IntelliMix_Core.Models;
using IntelliMix_Core.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;


namespace IntelliMix_Core.Controllers
{
    public class BillOfMaterialController : Controller
    {
        private readonly ITokenService _tokenService;

        public BillOfMaterialController(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        // ================================
        //  MAIN PAGE - BOM MASTER
        // ================================
        public async Task<IActionResult> Index(string filter = "all")
        {
            ViewBag.ApiStatus = HttpContext.Session.GetString("ApiStatus");

            var bomFlat = await GetBOMDetails() ?? new List<BillOfMaterial>();

            var bomDetails = bomFlat
                .GroupBy(x => x.Program_Code)
                .Select(g => new BillOfMaterial
                {
                    Program_Code = g.Key,
                    //NoOfSeq = g.SelectMany(x => x.Sequences ?? new List<Sequence>()).Count(),
                    Sequences = g.SelectMany(x => x.Sequences ?? new List<Sequence>()).ToList(),
                    _Created_By = g.Max(x => x._Created_By),
                    _Modified_By = g.Max(x => x._Modified_By),
                    _Modified_At = g.Max(x => x._Modified_At),
                    _Created_At = g.First()._Created_At != null ? Convert.ToDateTime(g.First()._Created_At.ToString()) : (DateTime?)null,
                    //_Modified_At = g.First()._Modified_At != null ? Convert.ToDateTime(g.First()._Modified_At.ToString()) : (DateTime?)null
                })
                .ToList();


            ViewBag.billOfMaterials = bomDetails;

            return View(bomDetails);
        }



        [HttpGet]
        public async Task<JsonResult> KneaderEvents()
        {
            var events = await GetKneaderEvents();
            return Json(events);
        }
        // ================================
        //  FETCH KNEADER EVENTS
        // ================================
        private async Task<List<KneaderEvents>> GetKneaderEvents()
        {
            string token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return new List<KneaderEvents>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("http://localhost:5254/api/v1/BillOfMaterial/GetKneaderEvents");
                if (!response.IsSuccessStatusCode)
                    return new List<KneaderEvents>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<KneaderEvents>>(json)
                       ?? new List<KneaderEvents>();
            }
        }

        // ================================
        //  FETCH BOM DETAILS (GET)
        // ================================
        [HttpGet]
        public async Task<JsonResult> BOMDetails()
        {
            var events = await GetBOMDetails();
            return Json(events);
        }

        private async Task<List<BillOfMaterial>> GetBOMDetails()
        {
            string token = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return new List<BillOfMaterial>();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync("http://localhost:5254/api/v1/BillOfMaterial/GetBOMDetails");
                if (!response.IsSuccessStatusCode)
                    return new List<BillOfMaterial>();

                var json = await response.Content.ReadAsStringAsync();

                // Deserialize flat list
                var flatList = JsonConvert.DeserializeObject<List<dynamic>>(json);
                if (flatList == null) return new List<BillOfMaterial>();

                // ✅ Group by program_code and combine sequences
                var grouped = flatList
                    .GroupBy(x => (string)x.program_code)
                    .Select(g => new BillOfMaterial
                    {
                        Program_Code = g.Key,
                        Sequences = g.Select(s => new Sequence
                        {
                            sequence_no = (string)s.sequence_no,
                            time = (int?)s.time,
                            temp = (int?)s.temperature,
                            rpm = (int?)s.rpm,
                            energy = (int?)s.energy,
                            mix_mode = (string)s.mix_mode,
                            event1 = (string)s.event1,
                            event2 = (string)s.event2,
                            event3 = (string)s.event3,
                            event4 = (string)s.event4,
                            remarks = (string)s.remarks
                        }).ToList(),

                        _Created_By = (string?)g.First()._created_by,

                        _Modified_By = (string?)g.First()._modified_by,

                        _Created_At = (DateTime?)g.First()._created_at,

                        _Modified_At = (DateTime?)g.First()._modified_at

                    })
                    .ToList();

                return grouped;
            }
        }
        // ================================
        //  SAVE BOM (POST)
        // ================================

        [HttpPost]
        public async Task<IActionResult> SaveBOM([FromBody] BillOfMaterial bom)
        {
            bool success = false;
            string message = "";
            string sentData = "";

            try
            {
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    message = "⚠️ Session expired. Please log in again.";
                    return Json(new { success, message });
                }

                // ✅ Build payload matching your API model
                var payload = new
                {
                    program_code = bom.Program_Code,
                    _created_by = HttpContext.Session.GetString("User") ?? "System",
                    _modified_by = HttpContext.Session.GetString("User") ?? "System",

                    _created_at = DateTime.Now,
                  
                    _modified_at = DateTime.Now,
                    _disable = 0,
                    sequence_no = "1",
                    Sequences = bom.Sequences.Select((s, i) => new
                    {
                        sequence_no = (i + 1).ToString(),
                        time = s.time,
                        temp = s.temp,
                        rpm = s.rpm,
                        energy = s.energy,
                        mix_mode = s.mix_mode,
                        event1 = s.event1,
                        event2 = s.event2,
                        event3 = s.event3,
                        event4 = s.event4,
                        remarks = s.remarks
                    }).ToList()
                };

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    sentData = json;
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://localhost:5254/api/v1/BillOfMaterial", content);
                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        message = "✅ BOM inserted successfully!";
                        success = true;
                    }
                    else
                    {
                        message = $"❌ Insert error:\n{result}\n\n📦 Sent Payload:\n{sentData}";
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"💥 Exception occurred: {ex.Message}\n\n📦 Sent Payload:\n{sentData}";
            }

            // Return both the message and the payload for debugging
            return Json(new { success, message });
        }
        // ================================
        //  DELETE BOM (POST)
        // ================================

        [HttpPost]
        public async Task<ActionResult> DeleteBOM(string program_code)
        {
            bool success = false;
            string message = "";
            if (string.IsNullOrEmpty(program_code))
                return Json(new { success, message = "Invalid Program Code" });

            try
            {
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    message = "⚠️ Session expired. Please log in again.";
                    return Json(new { success, message });
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var apiUrl = $"http://localhost:5254/api/v1/BillOfMaterial/Delete/{program_code}";
                    var response = await client.PostAsync(apiUrl, null);


                    if (response.IsSuccessStatusCode)
                    {
                        success = true;
                        message = "BOM deleted successfully";
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
        //  UPDATE BOM (POST)
        // ================================
        [HttpPost]
        public async Task<IActionResult> UpdateBOM([FromBody] BillOfMaterial bom)
        {
            bool success = false;
            string message = "";
            string sentData = "";

            try
            {
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    message = "⚠️ Session expired. Please log in again.";
                    return Json(new { success, message });
                }

                var payload = new
                {
                    program_code = bom.Program_Code,
                    
                    _created_at = DateTime.Now,
                    _created_by = HttpContext.Session.GetString("User") ?? "System",
                    _modified_by = HttpContext.Session.GetString("User") ?? "System",

                    _modified_at = DateTime.Now,
                    Sequences = bom.Sequences.Select((s, i) => new
                    {
                        sequence_no = (i + 1).ToString(),
                        time = s.time,
                        temp = s.temp,
                        rpm = s.rpm,
                        energy = s.energy,
                        mix_mode = s.mix_mode,
                        event1 = s.event1,
                        event2 = s.event2,
                        event3 = s.event3,
                        event4 = s.event4,
                        remarks = s.remarks
                    }).ToList()
                };

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    sentData = json;
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(
                            $"http://localhost:5254/api/v1/BillOfMaterial/Update/{bom.Program_Code}",
                            content
                        );

                    var result = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        success = true;
                        message = "✅ BOM updated successfully!";
                    }
                    else
                    {
                        message = $"❌ Update failed:\n{result}\n\n📦 Sent Payload:\n{sentData}";
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"💥 Exception occurred: {ex.Message}\n\n📦 Sent Payload:\n{sentData}";
            }

            return Json(new { success, message });
        }


        // ================================
        //  EXPORT CSV 
        // ================================
        [HttpGet]
        public async Task<ActionResult> ExportCsv(string filter = "all")
        {
            // Fetch BOM with nested sequence details
            var bomDetails = await GetBOMDetails();

            // Build CSV header
            var csv = new StringBuilder();
            csv.AppendLine("Program Code,Sequence No,Time,Temperature,RPM,Energy,Mix Mode,Event1,Event2,Event3,Event4,Remarks,Created At,Created By,Modified At,Modified By");

            foreach (var bom in bomDetails)
            {
                if (bom.Sequences != null && bom.Sequences.Any())
                {
                    foreach (var seq in bom.Sequences)
                    {
                        csv.AppendLine(
                            $"\"{bom.Program_Code}\"," +
                            $"\"{seq.sequence_no}\"," +
                            $"\"{seq.time}\"," +
                            $"\"{seq.temp}\"," +
                            $"\"{seq.rpm}\"," +
                            $"\"{seq.energy}\"," +
                            $"\"{seq.mix_mode}\"," +
                            $"\"{seq.event1}\"," +
                            $"\"{seq.event2}\"," +
                            $"\"{seq.event3}\"," +
                            $"\"{seq.event4}\"," +
                            $"\"{seq.remarks}\"," +
                            $"\"{bom._Created_At}\"," +
                            $"\"{bom._Created_By}\"," +
                            $"\"{bom._Modified_At}\"," +
                            $"\"{bom._Modified_By}\""
                        );
                    }
                }
                else
                {
                    // In case no sequence data is present for the BOM
                    csv.AppendLine(
                        $"\"{bom.Program_Code}\",\"-\",\"-\",\"-\",\"-\",\"-\",\"-\",\"-\",\"-\",\"-\",\"-\",\"-\",\"{bom._Created_At}\",\"{bom._Created_By}\",\"{bom._Modified_At}\",\"{bom._Modified_By}\""
                    );
                }
            }

            byte[] buffer = Encoding.UTF8.GetBytes(csv.ToString());
            return File(buffer, "text/csv", "BOM_Details.csv");
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
                string token = await _tokenService.GetAccessTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return Json(new { success, message = "⚠️ Session expired. Please log in again." });

                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var dataByProgram = new Dictionary<string, List<dynamic>>();
                    string headerLine = await reader.ReadLineAsync(); // skip header

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var cols = line.Split(',');

                        if (cols.Length < 15)
                        {
                            failedCount++;
                            continue;
                        }

                       
                        var vProgramCode = cols[0].Trim();
                        if (!dataByProgram.ContainsKey(vProgramCode))
                            dataByProgram[vProgramCode] = new List<dynamic>();

                     
                        int vSeqNo = dataByProgram[vProgramCode].Count + 1;

                        int ParseInt(string s) => int.TryParse(s?.Trim(), out var x) ? x : 0;
                        var vSequence = new
                        {
                            sequence_no = vSeqNo,
                            time = int.TryParse(cols[2].Trim(), out var vTime) ? vTime : 0,
                            temp = int.TryParse(cols[3].Trim(), out var vTemp) ? vTemp : 0,
                            rpm = int.TryParse(cols[4].Trim(), out var vRpm) ? vRpm : 0,
                            energy = int.TryParse(cols[5].Trim(), out var vEnergy) ? vEnergy : 0,
                            mix_mode = cols[6].Trim(),
                            event1 = cols[7].Trim(),
                            event2 = cols[8].Trim(),
                            event3 = cols[9].Trim(),
                            event4 = cols[10].Trim(),
                            remarks = cols[11].Trim()
                        };

                       

                        dataByProgram[vProgramCode].Add(vSequence);
                    }

                  
                    foreach (var kv in dataByProgram)
                    {
                        var payload = new
                        {
                            program_code = kv.Key,
                            _created_by = HttpContext.Session.GetString("User") ?? "System",
                            _modified_by = HttpContext.Session.GetString("User") ?? "System",

                            _created_at = DateTime.Now,
                        
                            _modified_at = DateTime.Now,
                            _disable = 0,
                            Sequences = kv.Value
                        };

                        var json = JsonConvert.SerializeObject(payload);
                        
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync("http://localhost:5254/api/v1/BillOfMaterial", content);

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

