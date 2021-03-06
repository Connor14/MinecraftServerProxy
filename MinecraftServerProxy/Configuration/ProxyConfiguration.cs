﻿using System;
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
        /// The IConfiguration section for the ProxyConfiguration (in appsettings.json, for example)
        /// </summary>
        public const string Section = "ProxyConfiguration";

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
        public Dictionary<string, ServerConfiguration> Servers { get; set; }

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

            Servers = new Dictionary<string, ServerConfiguration>();
        }
    }
}
