using KaneBlake.Basis.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Startup
{
    public class InstrumentStartup : IInstrumentStartup
    {
        private readonly DiagnosticAdapterProcessorObserver _observer;
        private readonly ILogger _logger;

        public InstrumentStartup(DiagnosticAdapterProcessorObserver observer, ILoggerFactory loggerFactory)
        {
            _observer = observer;
            _logger = loggerFactory.CreateLogger(typeof(InstrumentStartup));
        }

        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            DiagnosticListener.AllListeners.Subscribe(_observer);
            _logger.LogInformation("Started SkyAPM .NET Core Agent.");
        }

        public async Task StopAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogInformation("Stopped SkyAPM .NET Core Agent.");
            // ReSharper disable once MethodSupportsCancellation
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
