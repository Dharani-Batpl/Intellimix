using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntelliMix_Core.Services
{
    public class ApiHealthCheckerService : BackgroundService
    {
        private readonly ILogger<ApiHealthCheckerService> _logger;
        private readonly IMemoryCache _cache;

        public ApiHealthCheckerService(ILogger<ApiHealthCheckerService> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var apiStatus = await CheckApiHealthAsync();

                _logger.LogInformation($"[{DateTime.Now}] API Health: {apiStatus}");

                // ✅ Save to memory cache
                _cache.Set("ApiStatus", apiStatus, TimeSpan.FromMinutes(10));

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task<string> CheckApiHealthAsync()
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync("http://localhost:5254/health");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return result.Contains("Healthy", StringComparison.OrdinalIgnoreCase)
                        ? "Healthy"
                        : "Warning";
                }
                else
                {
                    return "Unhealthy";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API health check failed");
                return "Unhealthy";
            }
        }
    }
}
