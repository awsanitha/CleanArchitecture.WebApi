using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Infrastructure.Identity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace WebApi
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                try
                {
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                    await Infrastructure.Identity.Seeds.DefaultRoles.SeedAsync(userManager, roleManager);
                    await Infrastructure.Identity.Seeds.DefaultSuperAdmin.SeedAsync(userManager, roleManager);
                    await Infrastructure.Identity.Seeds.DefaultBasicUser.SeedAsync(userManager, roleManager);
                    Log.Information("Finished Seeding Default Data");
                    Log.Information("Application Starting");
                    host.Run();
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "An error occurred seeding the DB");
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)) //Uses Serilog instead of default .NET Logger
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
