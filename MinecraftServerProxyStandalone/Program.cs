using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinecraftServerProxy;
using MinecraftServerProxy.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxyStandalone
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Minecraft Server Proxy");
            Console.WriteLine("========================================");

            // Create a new Serilog logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Override the minimum level so Microsoft events are at a minimum of Information
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // Set up the Minecraft Server Proxy services
                // ProxyConfiguration is read from appsettings.json by default
                .UseMinecraftServerProxy()
                .UseSerilog(); // Configure Microsoft.Extensions.Hosting to use Serilog as its logger
    }
}
