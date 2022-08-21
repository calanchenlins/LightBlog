using K.Diagnostics.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace K.Diagnostics
{
    /// <summary>
    /// 支持属性绑定和对象绑定
    /// 性能: 对象、属性绑定(80ns) > Microsoft.Extensions.DiagnosticAdapter(140ns)
    /// </summary>
    public class DiagnosticAdapterProcessorObserver : DiagnosticProcessorObserver
    {
        public DiagnosticAdapterProcessorObserver(IEnumerable<IDiagnosticProcessor> tracingDiagnosticProcessors, ILoggerFactory loggerFactory) : base(tracingDiagnosticProcessors, loggerFactory)
        {
        }

        protected override IDisposable Subscribe(DiagnosticListener listener, IDiagnosticProcessor target)
        {
            var adapter = new DiagnosticSourceAdapter(target, _loggerFactory);
            return listener.Subscribe(adapter, adapter.IsEnabled);
        }
    }


}
