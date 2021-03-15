using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinecraftServerProxy.Configuration;
using MinecraftServerProxy.Packets;
using MinecraftServerProxy.Utility;
using Pipelines.Sockets.Unofficial;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxy
{
    public class ProxyServer : SocketServer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProxyServer> _logger;
        private readonly IOptionsMonitor<ProxyConfiguration> _proxyConfiguration;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ReferenceCounter _referenceCounter = new ReferenceCounter();

        // NOTE: IOptionsMonitor<ProxyConfiguration> will always have the most up-to-date configuration
        public ProxyServer(IServiceProvider serviceProvider, ILogger<ProxyServer> logger, IOptionsMonitor<ProxyConfiguration> proxyConfiguration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _proxyConfiguration = proxyConfiguration;
        }

        public new void Stop() => throw new InvalidOperationException("Please use StopAsync instead");

        /// <summary>
        /// Stops and waits for all ProxyConnections to complete.
        /// Once stopped, the ProxyServer cannot be restarted.
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            // Stop accepting connections
            base.Stop();

            // Cancel the ProxyServer cancellation token
            // This will also cancel all individual ProxyConnection tokens (since they became linked)
            _cancellationTokenSource.Cancel();

            // Complete the reference counter so no new tasks are allowed to start
            _referenceCounter.Complete();

            // Wait for the proxy task counter to return to 0. Once we are back at 0, we know there are no more active proxy connections
            // OR wait for an infinite delay to be cancelled by the given cancellation token
            await Task.WhenAny(_referenceCounter.WaitAsync(), Task.Delay(Timeout.Infinite, cancellationToken));
        }

        // Handle a new Minecraft client connection
        protected override Task OnClientConnectedAsync(in ClientConnection client)
        {
            var cancellationToken = _cancellationTokenSource.Token;

            // Exit immediately if already canceled
            cancellationToken.ThrowIfCancellationRequested();

            if (client.RemoteEndPoint is IPEndPoint ipEndPoint
                && client.Transport is SocketConnection clientToProxy
                // Try to increment the reference counter. If successful, continue. We now know we have at 1 more proxy connection
                && _referenceCounter.TryIncrement(out int newCount)
            )
            {
                var remoteEndpoint = ipEndPoint.ToString();

                _logger.LogInformation("Client {client} - connected to proxy - {count} client(s) total", remoteEndpoint, newCount);

                return HandleClientConnectionAsync(remoteEndpoint, clientToProxy, cancellationToken);
            }

            return Task.CompletedTask;
        }

        protected override void OnClientFaulted(in ClientConnection client, Exception exception)
        {
            _logger.LogError(exception, "Client {client} - faulted", client.RemoteEndPoint.ToString());

            base.OnClientFaulted(client, exception);
        }

        private async Task HandleClientConnectionAsync(string remoteEndpoint, SocketConnection clientToProxy, CancellationToken cancellationToken)
        {
            try
            {
                // Handle the proxy connection and wait for it to complete
                await HandleProxyAsync(remoteEndpoint, clientToProxy, cancellationToken);
            }
            finally
            {
                // Then decrement the reference counter to signal the proxy connection is no longer active
                int countRemaining = _referenceCounter.Decrement();

                _logger.LogInformation("Client {client} - disconnected from proxy and server - {count} client(s) remaining", remoteEndpoint, countRemaining);
            }
        }

        private async Task HandleProxyAsync(string remoteEndpoint, SocketConnection clientToProxy, CancellationToken cancellationToken)
        {
            // If the cancellation token has been canceled, exit immediately
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug("Client {client} - reading handshake", remoteEndpoint);

            // When a client first connects, read the handshaking packet data
            var packet = await clientToProxy.Input.ReadPacketAsync(cancellationToken);

            // Parse the data
            var handshakePacket = HandshakePacket.Create(packet);

            // Find the server the user wants to connect to
            // This will use the most up-to-date value of the configuration
            if (!_proxyConfiguration.CurrentValue.Servers.TryGetValue(handshakePacket.ServerAddress, out ServerConfiguration destination))
            {
                _logger.LogInformation("Client {client} - could not find server configuration: {serverConfiguration}", remoteEndpoint, handshakePacket.ServerAddress);
                return;
            }

            // Connect to the server
            using (var proxyToServer = await SocketConnection.ConnectAsync(new IPEndPoint(IPAddress.Parse(destination.IPAddress), destination.Port)))
            // Create a scope to resolve the logger from
            using (var scope = _serviceProvider.CreateScope())
            {
                _logger.LogInformation("Client {client} - connected to server at {ipAddress}:{port}", remoteEndpoint, destination.IPAddress, destination.Port);

                // Create the proxy connection between the Minecraft client and Minecraft server
                var proxiedConnectionLogger = scope.ServiceProvider.GetRequiredService<ILogger<ProxiedConnection>>();
                var proxiedConnection = new ProxiedConnection(remoteEndpoint, clientToProxy, proxyToServer, proxiedConnectionLogger);

                _logger.LogInformation("Client {client} - starting proxied connection", remoteEndpoint);

                // Start proxying and wait for the proxy task to complete
                await proxiedConnection.ProxyAsync(handshakePacket, cancellationToken);

                _logger.LogInformation("Client {client} - proxied connection finished", remoteEndpoint);
            }
        }
    }
}
