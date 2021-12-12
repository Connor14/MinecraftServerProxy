using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftServerProxy.Configuration
{
    /// <summary>
    /// Represents the connection information for a Minecraft server. Used by the proxy so it knows where to relay the TCP traffic. 
    /// 
    /// NOTE: The Hostname and Port of a ServerConfiguration instance should not be changed after creation. This object may be referenced by active connections.
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        /// The IP Address that the Minecraft server is running on.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// The Port that the Minecraft server is running on.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Creates an empty Minecraft server configuration.
        /// </summary>
        public ServerConfiguration() { }

        /// <summary>
        /// Creates a new Minecraft server configuration. 
        /// </summary>
        /// <param name="hostname">The Hostname or IP Address that the Minecraft server is running on.</param>
        /// <param name="port">The Port that the Minecraft server is running on.</param>
        public ServerConfiguration(string hostname, int port)
        {
            Hostname = hostname;
            Port = port;
        }
    }
}
