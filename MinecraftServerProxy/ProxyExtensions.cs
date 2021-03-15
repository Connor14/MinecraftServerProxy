using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinecraftServerProxy.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerProxy
{
    public static class ProxyExtensions
    {
        /// <summary>
        /// Sets up <see cref="ProxyWorker"/> to proxy Minecraft clients to servers.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IHostBuilder UseMinecraftServerProxy(this IHostBuilder builder)
        {
            return builder
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ProxyConfiguration>(hostContext.Configuration.GetSection(ProxyConfiguration.Section));

                    // Add the ProxyServer as a singleton
                    services.AddSingleton<ProxyServer>();

                    // Add the ProxyWorker background service
                    services.AddHostedService<ProxyWorker>();
                });
        }

    }
}
