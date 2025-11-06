using IntelliMix_Core.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace IntelliMix_Core.Services
{

    // Interface for Token Service
    public interface ITokenService
    {
        Task<string> GetAccessTokenAsync();
    }

    public class TokenService : ITokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Method to get access token
        public async Task<string> GetAccessTokenAsync()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
                return null;

            var token = session.GetString("AuthToken");
            if (!string.IsNullOrEmpty(token))
                return token;

            var username = session.GetString("User");
            var password = session.GetString("Password");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return null;

            try
            {
                // Make HTTP POST request to get token
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

                    // Store token in session
                    session.SetString("AuthToken", tokenResponse.AccessToken);
                    return tokenResponse.AccessToken;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
