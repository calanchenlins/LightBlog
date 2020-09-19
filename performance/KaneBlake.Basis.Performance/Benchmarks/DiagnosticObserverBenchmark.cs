using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DiagnosticAdapter;
using KaneBlake.Basis.Common.Diagnostics.Abstractions;
using KaneBlake.Basis.Common.Diagnostics;

namespace KaneBlake.Basis.Performance.Benchmarks
{
    public class CrashParamsT
    {
        public CrashParams bar { get; set; }
    }
    public class CrashParams
    {
        public string CallCount { get; set; }

        public string Baz { get; set; }
    }
    public class CrashDiagnosticProcessor : IDiagnosticProcessor
    {
        public string ListenerName => "Crash";
        public int CallCount { get; private set; }

        public int Baz { get; private set; }

        [DiagnosticAdapterName("Bar")]
        public void OnBar(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticAdapterName("Bar2")]
        public void OnBar2(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticAdapterName("Bar3")]
        public void OnBar3(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticAdapterName("Bar4")]
        public void OnBar4(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticAdapterName("Bar5")]
        public void OnBar5(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticAdapterName("Foo")]
        public void OnFoo([Object]CrashParams bar)
        {
            CallCount++;
        }
        [DiagnosticAdapterName("Foop")]
        public void OnFoo2([Property]CrashParams bar)
        {
            CallCount++;
        }
    }
    public class CrashDiagnosticProcessorMS : IDiagnosticProcessor
    {
        public string ListenerName => "CrashMS";
        public int CallCount { get; private set; }

        public int Baz { get; private set; }

        [DiagnosticName("Bar")]
        public void OnBar(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticName("Bar2")]
        public void OnBar2(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticName("Bar3")]
        public void OnBar3(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticName("Bar4")]
        public void OnBar4(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticName("Bar5")]
        public void OnBar5(CrashParams baz)
        {
            CallCount++;
        }
        [DiagnosticName("Foo")]
        public void OnFoo(CrashParams bar)
        {
            CallCount++;
        }

    }
    public class DiagnosticObserverBenchmark
    {
        private readonly DiagnosticListener _listener;
        private readonly DiagnosticListener _listenerMS;
        private readonly CrashParams _parm;//
        private readonly CrashParamsT _parmT;
        public DiagnosticObserverBenchmark()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DiagnosticProcessorObserver>();
            services.AddSingleton<DiagnosticAdapterProcessorObserver>();
            services.AddSingleton<IDiagnosticProcessor, CrashDiagnosticProcessorMS>();
            services.AddSingleton<IDiagnosticProcessor, CrashDiagnosticProcessor>();
            services.AddSingleton<ILoggerFactory,LoggerFactory>();
           
            var serviceProvider = services.BuildServiceProvider();
            var diagnosticObserver = serviceProvider.GetService<DiagnosticAdapterProcessorObserver>();
            var diagnosticObserverMS = serviceProvider.GetService<DiagnosticProcessorObserver>();
            DiagnosticListener.AllListeners.Subscribe(diagnosticObserver);
            DiagnosticListener.AllListeners.Subscribe(diagnosticObserverMS);
            _listener = new DiagnosticListener("Crash");
            _listenerMS = new DiagnosticListener("CrashMS");
            _parm = new CrashParams();
            _parm.Baz = "svsdvsdvs";
            _parm.CallCount = "sdvsdvsd";
            _parmT = new CrashParamsT();
            _parmT.bar = _parm;
        }

        [Benchmark]
        public void WriteMessage() {
            _listener.Write("Foo", _parm);
        }
        [Benchmark]
        public void WriteMessagePropAnonymous()
        {
            _listener.Write("Foop", new { bar = _parm });
        }
        [Benchmark]
        public void WriteMessageProp()
        {
            _listener.Write("Foop", _parmT);
        }

        [Benchmark]
        public void WriteMessageMS()
        {
            _listenerMS.Write("Foo", new { bar = _parm });
        }
        [Benchmark]
        public void WriteMessageMSAnonymous()
        {
            _listenerMS.Write("Foo", _parmT);
        }

    }
}
