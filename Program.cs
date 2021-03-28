using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PrScraper
{
    public class Config
    {
        public string? FilePath { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    Config config = hostContext.Configuration.GetSection("Config").Get<Config>();
                    services.AddSingleton(config);
                    services.AddHostedService<Worker>();
                });
    }
}
