using Microsoft.Extensions.DependencyInjection;
using MinecraftServerProxy.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerProxy
{
    public static class ProxyExtensions
    {
        /// <summary>
        /// Adds the provided <see cref="ProxyConfiguration" /> to Dependency Injection as a Singleton and adds the <see cref="ProxyWorker" /> as a Hosted Service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
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
