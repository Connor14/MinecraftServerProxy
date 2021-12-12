using Microsoft.Extensions.Logging;
using MinecraftServerProxy.Packets;
using MinecraftServerProxy.Utility;
using Pipelines.Sockets.Unofficial;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxy
{
    public class ProxiedConnection
    {
        private readonly ILogger<ProxiedConnection> _logger;

        private readonly string _remoteEndpoint;
        private readonly SocketConnection _clientToProxy;
        private readonly SocketConnection _proxyToServer;

        private CancellationTokenSource _cancellationTokenSource;

        public ProxiedConnection(string remoteEndpoint, SocketConnection clientToProxy, SocketConnection proxyToServer, ILogger<ProxiedConnection> logger)
        {
            _remoteEndpoint = remoteEndpoint;
            _clientToProxy = clientToProxy;
            _proxyToServer = proxyToServer;
            _logger = logger;
        }

        public async Task ProxyAsync(HandshakePacket handshakePacket, CancellationToken cancellationToken = default)
        {
            // Exit immediately if already canceled
            cancellationToken.ThrowIfCancellationRequested();

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _logger.LogDebug("Writing handshake for client {client}", _remoteEndpoint);

            // Write the handshake to the server
            await _proxyToServer.Output.WritePacketAsync(handshakePacket.CompletePacket, _cancellationTokenSource.Token);

            // Continuously copy bytes from Input to Output until either is completed or canceled.
            // Once copying is complete, cancel the cancellation token so the other task shuts down (if it hasn't already).
            var serverBound = ServerBound(cancellationToken);
            var clientBound = ClientBound(cancellationToken);

            _logger.LogDebug("Waiting for ServerBound and ClientBound links to complete for client {client}", _remoteEndpoint);

            // Wait for the ServerBound and ClientBound tasks to complete
            // This WILL throw an exception if one or both of the serverBound / clientBound tasks complete with an error.
            // ===== Note, however, that we are intentionally swallowing the exceptions from LinkToAsync since even normal disconnect situations yield read/write errors. =====
            await Task.WhenAll(serverBound, clientBound);
        }

        private async Task ServerBound(CancellationToken cancellationToken)
        {
            try
            {
                await _clientToProxy.Input.CopyToAsync(_proxyToServer.Output, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Exception in ServerBound link for client {client}", _remoteEndpoint);
            }
            finally
            {
                // If our link task completes for some reason, cancel the token so the client bound completes as well
                _cancellationTokenSource.Cancel();
            }
        }

        private async Task ClientBound(CancellationToken cancellationToken)
        {
            try
            {
                await _proxyToServer.Input.CopyToAsync(_clientToProxy.Output, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Exception in ClientBound link for client {client}", _remoteEndpoint);
            }
            finally
            {
                // If our link task completes for some reason, cancel the token so the server bound completes as well
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
