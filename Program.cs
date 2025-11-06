using IntelliMix_Core.Services;

namespace IntelliMix_Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllersWithViews();
            builder.Services.AddDistributedMemoryCache(); // Required for session
            builder.Services.AddMemoryCache();

            // Register IHttpContextAccessor for accessing HttpContext in services
            builder.Services.AddHttpContextAccessor();

            // Register your TokenService
            builder.Services.AddScoped<ITokenService, TokenService>();

            // Register ApiHealthCheckerService as a hosted service
            builder.Services.AddHostedService<IntelliMix_Core.Services.ApiHealthCheckerService>();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            // Middleware pipeline
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession(); // Must come BEFORE endpoints

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=User}/{action=Login}/{id?}");

            app.Run();
        }
    }
}
