using Microsoft.Extensions.DependencyInjection;
using MinecraftServerProxy.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerProxy
{
    public static class ProxyExtensions
    {
        public static IServiceCollection AddMinecraftServerProxy(this IServiceCollection services, ProxyConfiguration configuration)
        {
            // Add the configuration as a singleton
            services.AddSingleton<ProxyConfiguration>(configuration);

            // Add the ProxyWorker background service
            services.AddHostedService<ProxyWorker>();

            return services;
        }

    }
}
