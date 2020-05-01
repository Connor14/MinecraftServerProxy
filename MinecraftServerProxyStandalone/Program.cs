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
        // Default configuration path
        public static string ProxyConfigPath { get; private set; } = "config.json";

        // The configuration used by the proxy
        public static ProxyConfiguration ProxyConfiguration { get; private set; }

        public static int Main(string[] args)
        {
            Console.WriteLine("Minecraft Server Proxy Standalone");
            Console.WriteLine("========================================");

            // Create a new Serilog logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information) // Override the minimum level so Microsoft events are at a minimum of Information
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            // If an argument is provided, assume it is the path to the proxy configuration
            if (args.Length > 0)
            {
                ProxyConfigPath = Path.GetFullPath(args[0]); // Override the default config path
            }

            Log.Information("Using proxy configuration {path}", ProxyConfigPath);

            // Make sure the file exists
            if (!File.Exists(ProxyConfigPath))
            {
                Log.Error("Couldn't find the proxy configuration {path}", ProxyConfigPath);
                return 1;
            }

            // Get the configuration from file
            ProxyConfiguration = ProxyConfiguration.Load(ProxyConfigPath);

            // Validate
            if (!ProxyConfiguration.IsValid(ProxyConfiguration))
            {
                Log.Error("Proxy configuration is invalid.");
                return 1;
            }

            var cancellationTokenSource = new CancellationTokenSource();

            // Run the proxy worker
            Task proxyTask = CreateHostBuilder(args).Build().RunAsync(cancellationTokenSource.Token);

            // Listen for user input until a cancellation is requested or the task is completed (or the input is null and 'break' is called)
            while (!cancellationTokenSource.Token.IsCancellationRequested && !proxyTask.IsCompleted)
            {
                string input = Console.ReadLine();

                // CTRL + C or CTRL + Z will cause the input to be null
                // When CTRL + C is pressed, the proxy worker automatically shuts down before we call Cancel().
                // We end up cancelling the token so we stop looping and we make sure the proxy worker shuts down if CTRL+Z is pressed
                if (input is null)
                {
                    cancellationTokenSource.Cancel();
                    break;
                }
                // Continue if just whitespace
                else if (input.Trim() == string.Empty)
                {
                    continue;
                }

                switch (input.Trim().ToLower())
                {
                    case "reload":

                        // Get the configuration from file
                        // todo try-catch needed for write locks
                        ProxyConfiguration updatedConfiguration = ProxyConfiguration.Load(Program.ProxyConfigPath);

                        // Validate
                        if (ProxyConfiguration.IsValid(updatedConfiguration))
                        {
                            // Update the main proxy configuraiton if valid
                            // The update will be reflected in the ProxyWorker
                            ProxyConfiguration.Update(updatedConfiguration);

                            Log.Information("Proxy configuration updated.");
                        }
                        else
                        {
                            Log.Information("New proxy configuration was invalid.");
                        }

                        break;
                    case "stop":

                        cancellationTokenSource.Cancel();

                        break;
                    default:
                        Log.Error("Invalid command. Valid commands include: {commands}", "reload, stop");
                        break;
                }
            }

            Log.Information("Waiting for proxy to stop...");

            proxyTask.Wait();

            Log.Information("Proxy has stopped.");
            
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Set up the Minecraft Server Proxy HostedServices
                    services.AddMinecraftServerProxy(ProxyConfiguration);
                })
                .UseSerilog(); // Configure Microsoft.Extensions.Hosting to use Serilog as its logger
    }
}
