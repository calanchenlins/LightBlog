using KaneBlake.Basis.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.Basis.Extensions.Diagnostics
{
    public class DiagnosticProcessorService : IExecutionService
    {
        private readonly DiagnosticAdapterProcessorObserver _observer;

        private readonly ILogger _logger;

        private bool _disposed = true;
        private IDisposable _subscriber;

        public DiagnosticProcessorService(DiagnosticAdapterProcessorObserver observer, ILoggerFactory loggerFactory)
        {
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            _logger = loggerFactory?.CreateLogger(typeof(DiagnosticProcessorService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                _logger.LogWarning("Start DiagnosticProcessor Service.");
                _disposed = false;
                _subscriber = DiagnosticListener.AllListeners.Subscribe(_observer);
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_disposed)
            {
                _logger.LogWarning("Stop DiagnosticProcessor Service.");
                _disposed = true;
                _subscriber?.Dispose();
                _subscriber = null;
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}
