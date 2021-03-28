using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PrScraper
{
    public class Config
    {
        public string? FilePath { get; set; }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration config = hostContext.Configuration;

                    services.AddSingleton(config.GetSection("Config").Get<Config>());
                    services.AddLogging(loggingBuilder => loggingBuilder.AddFile(config.GetSection("Logging")));
                    services.AddHostedService<Worker>();
                });
    }
}
