using KaneBlake.Basis.Extensions.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Diagnostics
{
    public class DiagnosticProcessorService : IHostedService
    {
        private readonly DiagnosticAdapterProcessorObserver _observer;
        private readonly ILogger _logger;

        public DiagnosticProcessorService(DiagnosticAdapterProcessorObserver observer, ILoggerFactory loggerFactory)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _logger = loggerFactory?.CreateLogger(typeof(DiagnosticProcessorService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task StartAsync(CancellationToken cancellationToken= default)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            DiagnosticListener.AllListeners.Subscribe(_observer);
            _logger.LogInformation("Started SkyAPM .NET Core Agent.");
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Stopped SkyAPM .NET Core Agent.");
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
