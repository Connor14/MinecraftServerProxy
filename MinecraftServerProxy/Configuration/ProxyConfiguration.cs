using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MinecraftServerProxy.Configuration
{
    /// <summary>
    /// Represents a Minecraft Server Proxy's configuration.
    /// </summary>
    public class ProxyConfiguration
    {
        /// <summary>
        /// The IP Address that the proxy should listen on.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// The Port the proxy should listen on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The list of Minecraft server configurations that this proxy is responsible for.
        /// 
        /// The key of the dictionary is the IP Address or Hostname that is used by a client to connect to the proxy. The associated value is the server where the TCP traffic should be relayed.
        /// </summary>
        public ConcurrentDictionary<string, ServerConfiguration> Servers { get; set; }

        /// <summary>
        /// Creates an empty Minecraft server proxy configuration.
        /// </summary>
        public ProxyConfiguration() { }

        /// <summary>
        /// Creates a new configuration for the Minecraft Server Proxy to use.
        /// </summary>
        /// <param name="ipAddress">The IP Address that the proxy will listen on.</param>
        /// <param name="port">The Port that the proxy will listen on.</param>
        public ProxyConfiguration(string ipAddress, int port)
        {
            IPAddress = ipAddress;
            Port = port;

            Servers = new ConcurrentDictionary<string, ServerConfiguration>();
        }

        /// <summary>
        /// Updates this configuration using the provided configuration.
        /// </summary>
        /// <param name="updated"></param>
        public void Update(ProxyConfiguration updated)
        {
            // todo should I do any locking so that I don't accidentally cause a race condition?
            IPAddress = updated.IPAddress;
            Port = updated.Port;

            Servers = updated.Servers;
        }

        /// <summary>
        /// Loads a <see cref="ProxyConfiguration" /> from a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ProxyConfiguration Load(string path)
        {
            // Read the json file
            string json = File.ReadAllText(path);

            // Get the configuration from JSON
            var configuration = JsonSerializer.Deserialize<ProxyConfiguration>(json);

            return configuration;
        }

        /// <summary>
        /// Returns true if all of the properties on the provided <see cref="ProxyConfiguration" /> are valid.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static bool IsValid(ProxyConfiguration configuration)
        {
            return configuration != null
                && !string.IsNullOrWhiteSpace(configuration.IPAddress)
                && configuration.Port > 0
                && configuration.Servers != null;
        }
    }
}
