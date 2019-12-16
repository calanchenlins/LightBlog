using KaneBlake.Basis.Extensions.Diagnostics.Abstractions;
using KaneBlake.Basis.Extensions.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace KaneBlake.Basis.Extensions.Diagnostics
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

        public DiagnosticProcessorObserver(IEnumerable<IDiagnosticProcessor> DiagnosticProcessors
            , ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(DiagnosticAdapterProcessorObserver));
            _loggerFactory = loggerFactory;
            _tracingDiagnosticProcessors = DiagnosticProcessors ??
                                           throw new ArgumentNullException(nameof(DiagnosticProcessors));
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            foreach (var diagnosticProcessor in _tracingDiagnosticProcessors.Distinct(x => x.ListenerName))
            {
                if (listener.Name == diagnosticProcessor.ListenerName)
                {
                    Subscribe(listener, diagnosticProcessor);
                    _logger.LogInformation(
                    $"Loaded diagnostic listener [{diagnosticProcessor.ListenerName}].");
                }
            }
        }

        protected virtual void Subscribe(DiagnosticListener listener,
            IDiagnosticProcessor tracingDiagnosticProcessor)
        {
            listener.SubscribeWithAdapter(tracingDiagnosticProcessor);
        }
    }
}
