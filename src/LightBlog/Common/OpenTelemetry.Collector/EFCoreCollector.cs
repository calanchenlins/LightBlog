using System;
using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenTelemetry.Instrumentation;

namespace LightBlog.Common.OpenTelemetry.Collector
{
    public class EFCoreCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber diagnosticSourceSubscriber;

        public EFCoreCollector(Tracer tracer)
        {
            this.diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new EFCoreListener("Microsoft.AspNetCore", tracer), null);
            this.diagnosticSourceSubscriber.Subscribe();
        }

        public void Dispose()
        {
            this.diagnosticSourceSubscriber?.Dispose();
        }
    }
}
