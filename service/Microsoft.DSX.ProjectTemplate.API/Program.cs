using Microsoft.AspNetCore.Hosting;
using Microsoft.DSX.ProjectTemplate.Data;
using Microsoft.DSX.ProjectTemplate.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Microsoft.DSX.ProjectTemplate.API
{
    /// <summary>
    /// The main class of the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method where execution begins.
        /// </summary>
        /// <param name="args">Injected by the operating system.</param>
        public static void Main(string[] args)
        {
            IHost host = CreateHostBuilder(args).Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                RunDatabaseMigrations(host, logger);
                host.Run();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates a host builder for this API.
        /// </summary>
        /// <param name="args">Arguments for the web host builder.</param>
        /// <returns>A fully configured host builder.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .ConfigureAppConfiguration((builderContext, config) =>
                 {
#if DEBUG
                     var basePath = ConfigExtensions.GetSettingsDirectory();
#else
                     var basePath = Directory.GetCurrentDirectory();
#endif
                     config.SetBasePath(basePath);
                     config.AddJsonFile("appsettings.json", optional: false);
                 });
        }

        private static void RunDatabaseMigrations(IHost host, ILogger logger)
        {
            logger.LogInformation("Running database migrations.");

            using (var serviceScope = host.Services.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<ProjectTemplateDbContext>();
                context.Database.Migrate();
            }

            logger.LogInformation("Completed database migrations.");
        }
    }
}
