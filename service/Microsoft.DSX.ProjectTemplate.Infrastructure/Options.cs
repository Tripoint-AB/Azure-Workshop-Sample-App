using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        public static Options LoadOptions(this IServiceCollection services, HostBuilderContext context)
        {
            return LoadOptions(services, context.Configuration);
        }

        public static Options LoadOptions(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection("AppSettings").Get<Options>();
            services.AddSingleton(options);
            return options;
        }
    }

}