using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinecraftServerProxy.Configuration;
using MinecraftServerProxy.Packets;
using MinecraftServerProxy.Utility;

namespace MinecraftServerProxy
{
    public class ProxyWorker : BackgroundService
    {
        private readonly ILogger<ProxyWorker> _logger;

        private readonly ProxyConfiguration _proxyConfiguration;

        private readonly TcpListener _tcpListener;

        public ProxyWorker(ILogger<ProxyWorker> logger, ProxyConfiguration proxyConfiguration)
        {
            _logger = logger;
            _proxyConfiguration = proxyConfiguration;

            // todo maybe this should be created in StartAsync
            _tcpListener = new TcpListener(IPAddress.Parse(proxyConfiguration.IPAddress), proxyConfiguration.Port);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting at: {time}", DateTimeOffset.Now);

            _tcpListener.Start();

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);

            _tcpListener.Stop();

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker executing at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for a new client to connect
                TcpClient client = await _tcpListener.AcceptTcpClientAsync();

                // Handle the connection in the background
                Task.Factory.StartNew(() => HandleClientConnection(client), TaskCreationOptions.LongRunning);
            }

            _logger.LogInformation("Worker finished executing at: {time}", DateTimeOffset.Now);
        }

        public void HandleClientConnection(TcpClient clientToHubClient)
        {
            try
            {
                var clientToHubStream = clientToHubClient.GetStream();
                byte[] bytes = clientToHubStream.ReadBytes();

                if (bytes is null || bytes.Length == 0)
                    return;

                // If the Packet is not the handshaking packet, return
                if (Packet.GetPacketID(bytes) != 0)
                    return;

                var handshakePacket = new HandshakePacket(bytes);

                _logger.LogInformation(handshakePacket.ServerAddress);

                var server = _proxyConfiguration.Servers[handshakePacket.ServerAddress];

                // Connect to server
                TcpClient hubToServerClient = new TcpClient(server.IPAddress, server.Port);

                NetworkStream hubToServerStream = hubToServerClient.GetStream();

                // Write the handshake to the server
                hubToServerStream.WriteBytes(handshakePacket.Bytes);

                // Get the server handshake response
                byte[] serverResponse = hubToServerStream.ReadBytes();

                // Write handshake response to client
                clientToHubStream.WriteBytes(serverResponse);

                // Status
                // Don't need to keep the connection alive
                if (handshakePacket.NextState == 1)
                {
                    // Read client status request
                    byte[] clientStatusRequest = clientToHubStream.ReadBytes();

                    // Write status request to server
                    hubToServerStream.WriteBytes(clientStatusRequest);

                    // Read status request from server
                    byte[] serverStatusResponse = hubToServerStream.ReadBytes();

                    // Write status request to client
                    clientToHubStream.WriteBytes(serverStatusResponse);

                    // Cleanup
                    hubToServerStream.Dispose();
                    hubToServerClient.Dispose();
                    clientToHubStream.Dispose();
                    clientToHubClient.Dispose();
                }
                // Login (and beyond)
                // Keep the connection alive
                else if (handshakePacket.NextState == 2)
                {
                    // Client -> Server loop
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            // Continue reading/writing received bytes until close
                            while (clientToHubClient.Connected && hubToServerClient.Connected)
                            {
                                byte[] clientToHubBytes = clientToHubStream.ReadBytes();

                                hubToServerStream.WriteBytes(clientToHubBytes);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Exception in Client -> Server");
                        }
                        finally
                        {
                            // Cleanup
                            hubToServerStream.Dispose();
                            hubToServerClient.Dispose();
                            clientToHubStream.Dispose();
                            clientToHubClient.Dispose();
                        }

                    }, TaskCreationOptions.LongRunning);

                    // Server -> Client loop
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            // Continue reading/writing received bytes until close
                            while (clientToHubClient.Connected && hubToServerClient.Connected)
                            {
                                byte[] hubToServerBytes = hubToServerStream.ReadBytes();

                                clientToHubStream.WriteBytes(hubToServerBytes);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Exception in Server -> Client");
                        }
                        finally
                        {
                            // Cleanup
                            hubToServerStream.Dispose();
                            hubToServerClient.Dispose();
                            clientToHubStream.Dispose();
                            clientToHubClient.Dispose();
                        }

                    }, TaskCreationOptions.LongRunning);
                }
                else
                {
                    throw new Exception("Unknown NextState in HandshakePacket");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception in HandleClientConnection");
            }
        }
    }
}
