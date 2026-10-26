using System;
using System.Threading.Tasks;
using Application;
using Application.Interfaces;
using Infrastructure.Identity;
using Infrastructure.Identity.Models;
using Infrastructure.Persistence;
using Infrastructure.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using WebApi.Extensions;
using WebApi.Services;

namespace WebApi
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            //Read Configuration from appSettings
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            //Initialize Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog(); //Uses Serilog instead of default .NET Logger

            var _config = builder.Configuration;
            builder.Services.AddApplicationLayer();
            builder.Services.AddIdentityInfrastructure(_config);
            builder.Services.AddPersistenceInfrastructure(_config);
            builder.Services.AddSharedInfrastructure(_config);
            builder.Services.AddSwaggerExtension();
            builder.Services.AddControllers();
            builder.Services.AddApiVersioningExtension();
            builder.Services.AddHealthChecks();
            builder.Services.AddScoped<IAuthenticatedUserService, AuthenticatedUserService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSwaggerExtension();
            app.UseErrorHandlingMiddleware();
            app.UseHealthChecks("/health");
            app.MapControllers();
            // Terminal handler for UseExceptionHandler("/Error") re-execution.
            // The original netcoreapp3.1 app configured UseExceptionHandler("/Error") but never
            // mapped an /Error route, so unhandled production exceptions surfaced as a 404.
            // This handler is an intentional divergence from that latent defect: it returns a
            // 500 ProblemDetails response. Mapped for all HTTP verbs so that HEAD/PATCH/OPTIONS
            // re-execution is covered as well.
            app.MapMethods("/Error",
                new[] { "GET", "POST", "PUT", "DELETE", "HEAD", "PATCH", "OPTIONS" },
                (HttpContext context) =>
                Results.Problem(
                    title: "An unexpected error occurred.",
                    statusCode: StatusCodes.Status500InternalServerError));

            using (var scope = app.Services.CreateScope())
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
                    app.Run();
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
    }
}
