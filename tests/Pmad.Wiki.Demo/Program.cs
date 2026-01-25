using System.Security.Claims;
using AspNet.Security.OpenId.Steam;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Pmad.Wiki.Demo.Entities;
using Pmad.Wiki.Demo.Services;
using Pmad.Wiki.Services;

namespace Pmad.Wiki.Demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddScoped<IWikiUserService, DemoWikiUserService>();
            builder.Services.AddLocalization();

            builder.Services.AddControllersWithViews()
                .AddViewLocalization()
                .AddWiki(new WikiOptions()
                {
                    AllowAnonymousViewing = true
                });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = SteamAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddSteam(s =>
            {
                s.ApplicationKey = builder.Configuration.GetValue<string>("SteamKey");
            });

            builder.Services.AddAuthorization(options =>
            {
                var admins = builder.Configuration.GetSection("Admins").Get<string[]>() ?? Array.Empty<string>();
                options.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypes.NameIdentifier, admins));
            });

            builder.Services.AddDbContext<DemoContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString(nameof(DemoContext))));

            var app = builder.Build();

            EnsureDatabaseCreated(app);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseWikiStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapWiki();

            app.UseCookiePolicy(new CookiePolicyOptions()
            {
                HttpOnly = HttpOnlyPolicy.Always,
                Secure = CookieSecurePolicy.SameAsRequest,
                MinimumSameSitePolicy = SameSiteMode.Lax
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        private static void EnsureDatabaseCreated(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DemoContext>();
                dbContext.Database.Migrate();
            }
        }
    }
}
