using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.DSX.ProjectTemplate.Infrastructure
{
    public class Options
    {
        public AppSettings AppSettings { get; set; } 
        public Logging Logging { get; set; } 
        public string AllowedHosts { get; set; }
    }
    
    public class AppSettings    {
        public ConnectionStrings ConnectionStrings { get; set; } 
        public Dependencies Dependencies { get; set; } 
    }
    public class ConnectionStrings    {
        public string Database { get; set; } 
    }

    public class Dependencies    {
        public string ServiceBus { get; set; } 
        public string Topic { get; set; } 
    }

    public class LogLevel    {
        public string Default { get; set; } 
    }

    public class Logging    {
        public LogLevel LogLevel { get; set; } 
    }

    public static class ConfigExtensions
    {
        public static AppSettings LoadOptions(this IServiceCollection services, HostBuilderContext context)
        {
            return LoadOptions(services, context.Configuration);
        }

        public static AppSettings LoadOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<AppSettings>();
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
            var options = configuration.GetSection("AppSettings").Get<AppSettings>();
            services.AddSingleton(options);
            return options;
        }

        public static string GetSettingsDirectory()
        {
            string path = "";
            string assemblylocation = "";
            string directoryName = "";
            string parentDirectory = "";

            try
            {
                assemblylocation = Assembly.GetEntryAssembly().Location;
                directoryName = Path.GetDirectoryName(assemblylocation);
                parentDirectory = Directory.GetParent(directoryName).Parent.Parent.Parent.FullName;
                path = Path.Combine(parentDirectory, "Solution Items");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception when trying to get settings directory. Assemblylocation:{assemblylocation} DirecotryName: {directoryName}  ParentDirectory:{parentDirectory}");
                Console.WriteLine($"Exception Message: {ex.Message}. Exception {ex.StackTrace}");
                Console.WriteLine($"Fallingback to current directory.");
                path = Directory.GetCurrentDirectory(); //falback to currentdirectory
            }

            return path;
        }
    }

}