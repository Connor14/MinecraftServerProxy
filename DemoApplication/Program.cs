using Microsoft.Extensions.Hosting;
using MinecraftServerProxy;
using MinecraftServerProxy.Configuration;
using System;

namespace DemoApplication
{
    public class Program
    {
        private static ProxyConfiguration configuration;

        public static void Main(string[] args)
        {
            // Create the proxy configuration
            configuration = new ProxyConfiguration("127.0.0.1", 25565);
            configuration.Servers["connor.localhost"] = new ServerConfiguration("127.0.0.1", 25570);
            configuration.Servers["matt.localhost"] = new ServerConfiguration("127.0.0.1", 25571);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Set up the Minecraft Server Proxy HostedServices
                    services.AddMinecraftServerProxy(configuration);
                });
    }
}
