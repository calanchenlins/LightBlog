using KaneBlake.Extensions.Diagnostics.Abstractions;
using KaneBlake.Extensions.Pipelines;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KaneBlake.Extensions.Diagnostics
{
    /// <summary>
    /// 默认实现使用 Microsoft.Extensions.DiagnosticAdapter 作为适配器
    /// 方法参数只支持属性绑定(匿名对象和非匿名对象在性能上基本无差别)
    /// </summary>
    public class DiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {
        private readonly ILogger _logger;
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IEnumerable<IDiagnosticProcessor> _tracingDiagnosticProcessors;

        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public DiagnosticProcessorObserver(IEnumerable<IDiagnosticProcessor> DiagnosticProcessors
            , ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(DiagnosticAdapterProcessorObserver));
            _loggerFactory = loggerFactory;
            _tracingDiagnosticProcessors = DiagnosticProcessors ??
                                           throw new ArgumentNullException(nameof(DiagnosticProcessors));
        }

        /// <summary>
        /// called when Observer of DiagnosticListener is disposed
        /// </summary>
        public void OnCompleted()
        {
            _subscriptions.ForEach(x => x.Dispose());
            _subscriptions.Clear();
        }

        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// Called when a new DiagnosticListener gets created
        /// </summary>
        /// <param name="listener"></param>
        public void OnNext(DiagnosticListener listener)
        {
            foreach (var diagnosticProcessor in _tracingDiagnosticProcessors.DistinctBy(x => x.ListenerName))
            {
                if (listener.Name == diagnosticProcessor.ListenerName)
                {
                    var subscription =  Subscribe(listener, diagnosticProcessor);
                    _subscriptions.Add(subscription);
                    _logger.LogInformation(
                    $"Loaded diagnostic listener [{diagnosticProcessor.ListenerName}].");
                }
            }
        }

        protected virtual IDisposable Subscribe(DiagnosticListener listener,
            IDiagnosticProcessor tracingDiagnosticProcessor)
        {
            return listener.SubscribeWithAdapter(tracingDiagnosticProcessor);
        }
    }
}
