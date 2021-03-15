using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinecraftServerProxy.Configuration;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftServerProxy
{
    public class ProxyWorker : BackgroundService
    {
        private readonly ILogger<ProxyWorker> _logger;

        private readonly IOptions<ProxyConfiguration> _configuration;
        private readonly ProxyServer _proxyServer;

        // NOTE: IOptions<ProxyConfiguration> is read once and not updated
        public ProxyWorker(ILogger<ProxyWorker> logger, IOptions<ProxyConfiguration> configuration, ProxyServer server)
        {
            _logger = logger;
            _configuration = configuration;
            _proxyServer = server;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            string ipAddress = _configuration.Value.IPAddress;
            int port = _configuration.Value.Port;

            _logger.LogInformation("Starting proxy on {ipAddress}:{port}", ipAddress, port);

            // Start the ProxyServer
            _proxyServer.Listen(new IPEndPoint(IPAddress.Parse(ipAddress), port));

            return base.StartAsync(cancellationToken);
        }

        // Keep the BackgroundService running until application shut down
        // The stoppingToken is triggered when StopAsync is called
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Delay(Timeout.Infinite, stoppingToken);

        // The cancellationToken is triggered when StopAsync has taken too long to shut down (when shutdown should no longer be graceful)
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping proxy");

            // Stop the ProxyServer
            // ProxyServer is Disposed by the ServiceProvider. We shouldn't dispose
            await _proxyServer.StopAsync(cancellationToken);

            await base.StopAsync(cancellationToken);
        }
    }
}
