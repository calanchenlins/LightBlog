using KaneBlake.Basis.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Hosting
{
    internal class InstrumentationHostedService : IHostedService
    {
        private readonly ILogger _logger;

        private readonly IEnumerable<IExecutionService> _services;

        public InstrumentationHostedService(ILoggerFactory loggerFactory, IEnumerable<IExecutionService> services)
        {
            _logger = loggerFactory?.CreateLogger<InstrumentationHostedService>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing ...");
            foreach (var service in _services)
                await service.StartAsync(cancellationToken);
            _logger.LogInformation("Started Extension Services.");
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            foreach (var service in _services)
                await service.StopAsync(cancellationToken);
            _logger.LogInformation("Stopped Extension Services.");
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
