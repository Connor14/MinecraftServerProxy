using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftServerProxy.Configuration;
using MinecraftServerProxy.Utility;
using SimpleTcp;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxy
{
    /// <summary>
    /// A client that the proxy uses to communicate with a Minecraft server
    /// </summary>
    public class ProxyClient : TcpClient
    {
        /// <summary>
        /// The IpPort of the Minecraft client whose data is being proxied to a Minecraft server
        /// </summary>
        public string ProxiedClientIpPort { get; set; }

        public ProxyClient(string proxiedClientIpPort, string serverIpOrHostname, int port)
            : base(serverIpOrHostname, port, false, null, null)
        {
            ProxiedClientIpPort = proxiedClientIpPort;
        }
    }

    public class ProxyWorker : BackgroundService
    {
        private readonly ILogger<ProxyWorker> _logger;

        private readonly ProxyConfiguration _proxyConfiguration;

        private readonly TcpServer _proxyServer;

        private readonly ConcurrentDictionary<string, ProxyClient> _proxyClients;

        public ProxyWorker(ILogger<ProxyWorker> logger, ProxyConfiguration proxyConfiguration)
        {
            _logger = logger;
            _proxyConfiguration = proxyConfiguration;
            _proxyClients = new ConcurrentDictionary<string, ProxyClient>();

            _proxyServer = new TcpServer(proxyConfiguration.IPAddress, proxyConfiguration.Port, false, null, null);
            _proxyServer.ClientConnected += ProxyServer_ClientConnected;
            _proxyServer.ClientDisconnected += ProxyServer_ClientDisconnected;
            _proxyServer.DataReceived += ProxyServer_DataReceived;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting at: {time}", DateTimeOffset.Now);

            _proxyServer.Start();

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);

            _proxyServer.Dispose();

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker executing at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, stoppingToken);
                }
                catch (TaskCanceledException taskCanceledException)
                {

                }
            }

            _logger.LogInformation("Worker finished executing at: {time}", DateTimeOffset.Now);
        }

        /// <summary>
        /// New Minecraft client connected to proxy
        /// </summary>
        private void ProxyServer_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            _logger.LogInformation("Client [{client}] connected", e.IpPort);

            // IF the new connection doesn't already have a proxy client configured
            // THEN create a new dictionary entry but don't create the proxy client yet. 
            // We need to create the proxy client after parsing the handshaking packet
            // todo Potential race condition? Or is it not possible to have a conflict because IpPort will always be unique?
            if (!_proxyClients.ContainsKey(e.IpPort))
            {
                _proxyClients[e.IpPort] = null;
            }
        }

        /// <summary>
        /// Minecraft client disconnected from proxy
        /// </summary>
        private void ProxyServer_ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            _logger.LogInformation("Client [{client}] disconnected: {reason}", e.IpPort, e.Reason.ToString());

            // Try to remove the proxy client associated with this IpPort so that we can dispose it
            if (_proxyClients.TryRemove(e.IpPort, out ProxyClient removedProxyClient))
            {
                removedProxyClient.Dispose();
            }
        }

        /// <summary>
        /// Data received by the ProxyServer from a Minecraft client
        /// </summary>
        private void ProxyServer_DataReceived(object sender, DataReceivedFromClientEventArgs e)
        {
            // Try to get an existing ProxyClient associated with this IpPort
            bool didFindProxyClient = _proxyClients.TryGetValue(e.IpPort, out ProxyClient foundProxyClient);

            // IF the client sending the data has a dictionary entry but the ProxyClient is null
            // THEN we have connected but we haven't parsed this client's handshaking packet yet
            if (didFindProxyClient && foundProxyClient is null)
            {
                // Parse the packet based on the given data
                // See https://wiki.vg/Protocol#Packet_format and https://wiki.vg/Protocol#Handshaking for more details
                int offset = 0;

                // Length of the PacketID and Data
                int length = e.Data.ReadNextVarInt(ref offset);

                // IF the length of the data received is less than the length of the Length + PacketID + Data
                // THEN, log an error and return
                if (e.Data.Length < length + offset)
                {
                    _logger.LogError("Expected a data length of at least [{expectedLength}] but received [{receivedLength}]", length + offset, e.Data.Length);
                    return;
                }

                // Packet ID
                int packetID = e.Data.ReadNextVarInt(ref offset);

                // IF the packet isn't the handshaking packet
                // THEN log an error and return
                if (packetID != 0)
                {
                    _logger.LogError("Expected a packet ID of [0] but got [{packetID}]", packetID);
                    return;
                }

                // Protocol Version
                int protocolVersion = e.Data.ReadNextVarInt(ref offset);

                // Server Address (the address the user typed in to the Minecraft multiplayer menu)
                string serverAddress = e.Data.ReadNextString(ref offset);

                // Port (the port the user typed into the Minecraft multiplayer menu)
                ushort port = e.Data.ReadNextUnsignedShort(ref offset);

                // Next State (status, login)
                int nextstate = e.Data.ReadNextVarInt(ref offset);

                // IF this server address is associated with a Minecraft server
                // THEN create a proxy client to connect to the server
                if (_proxyConfiguration.Servers.TryGetValue(serverAddress.Trim(), out ServerConfiguration serverConfiguration))
                {
                    ProxyClient proxyClient = new ProxyClient(e.IpPort, serverConfiguration.IPAddress, serverConfiguration.Port);
                    proxyClient.DataReceived += ProxyClient_DataReceived;

                    proxyClient.Connect();

                    // Assign the created proxy client with the IpPort of the Minecraft client being proxied
                    _proxyClients[e.IpPort] = proxyClient;

                    // Send data from proxy to server
                    proxyClient.Send(e.Data);
                }
                // OTHERWISE, log an error and return
                else
                {
                    _logger.LogError("Couldn't find a Minecraft server for [{serverAddress}]", serverAddress);
                    return;
                }
            }
            // OTHERWISE, IF we found a proxy client, THEN send the data immediately without parsing
            else if (didFindProxyClient)
            {
                // Send data from proxy to server
                foundProxyClient.Send(e.Data);
            }
        }

        /// <summary>
        /// Data received by a ProxyClient from a Minecraft server
        /// </summary>
        private void ProxyClient_DataReceived(object sender, DataReceivedFromServerEventArgs e)
        {
            ProxyClient client = (ProxyClient)sender;

            // Send the data received by the proxy client to a particular Minecraft client via the proxy server
            _proxyServer.Send(client.ProxiedClientIpPort, e.Data);
        }
    }
}
