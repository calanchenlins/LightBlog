using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace K.Diagnostics
{
    public class DiagnosticProcessorHostedService : IHostedService
    {
        private readonly IOptionsMonitor<DiagnosticOptions> _optionsMonitor;

        private readonly DiagnosticAdapterProcessorObserver _observer;

        private readonly ILogger _logger;

        private bool _disposed = true;

        private IDisposable? _subscriber;

        private IDisposable? _cts;



        public DiagnosticProcessorHostedService(IOptionsMonitor<DiagnosticOptions> optionsMonitor, DiagnosticAdapterProcessorObserver observer, ILoggerFactory loggerFactory)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            // scoped ILogger<> or single ILoggerFactory
            _logger = loggerFactory?.CreateLogger(typeof(DiagnosticProcessorHostedService)) ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            _cts = _optionsMonitor.OnChange(OptionsListener);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _cts?.Dispose();
            UnSubscribe();
            return Task.CompletedTask;
        }

        private void OptionsListener(DiagnosticOptions option, string name)
        {
            _logger.LogWarning("AppOptions Has Changed!");
            if (option.EnableDiagnostics)
            {
                Subscribe();
            }
            else
            {
                UnSubscribe();
            }
        }
        private void Subscribe()
        {
            if (_disposed)
            {
                _logger.LogWarning("Start DiagnosticProcessor Hosted Service.");
                _disposed = false;
                _subscriber = DiagnosticListener.AllListeners.Subscribe(_observer);
            }

        }
        private void UnSubscribe()
        {
            if (!_disposed)
            {
                _logger.LogWarning("Stop DiagnosticProcessor Hosted Service.");
                _disposed = true;
                _subscriber?.Dispose();
                _subscriber = null;
            }
        }
    }

}
