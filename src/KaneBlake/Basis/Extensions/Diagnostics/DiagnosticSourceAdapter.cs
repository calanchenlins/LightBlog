using System;
using System.Collections.Generic;
using System.Reflection;
using KaneBlake.Basis.Extensions.Diagnostics.Abstractions;
using Microsoft.Extensions.Logging;

namespace KaneBlake.Basis.Extensions.Diagnostics
{
    public class DiagnosticSourceAdapter : IObserver<KeyValuePair<string, object>>
    {
        private readonly Listener _listener;
        private readonly ILogger _logger;

        public DiagnosticSourceAdapter(IDiagnosticProcessor tracingDiagnosticProcessor, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(DiagnosticSourceAdapter));
            _listener = EnlistTarget(tracingDiagnosticProcessor);
        }
        private static Listener EnlistTarget(IDiagnosticProcessor target)
        {
            var listener = new Listener(target);

            //var methodInfos = target.GetType().GetMethods();//获取所有公开的方法(包括继承方法)

            var typeInfo = target.GetType().GetTypeInfo();//获取所有方法(包括私有方法，不包括继承方法)
            var methodInfos = typeInfo.DeclaredMethods;
            foreach (var methodInfo in methodInfos)
            {
                var diagnosticNameAttribute = methodInfo.GetCustomAttribute<DiagnosticAdapterName>();
                if (diagnosticNameAttribute != null)
                {
                    listener.Subscriptions.TryAdd(diagnosticNameAttribute.Name,new DiagnosticMethodSubscription(target, methodInfo, diagnosticNameAttribute.Name));
                }
            }

            return listener;
        }
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public bool IsEnabled(string diagnosticName)
        {
            if (_listener.Subscriptions.Count == 0)
            {
                return false;
            }

            return
                _listener.Subscriptions.ContainsKey(diagnosticName);
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (_listener.Subscriptions.TryGetValue(value.Key, out DiagnosticMethodSubscription subscription))
            {
                try 
                {
                    subscription.Invoke(value.Key, value.Value);
                }
                catch (Exception exception) 
                {
                    _logger.LogError("Invoke diagnostic method[{p1}] exception.", value.Key,exception);
                }
                
            }
        }


        private class Listener
        {
            public Listener(object target)
            {
                Target = target;
                Subscriptions = new Dictionary<string, DiagnosticMethodSubscription>(StringComparer.Ordinal);
            }

            public object Target { get; }

            public Dictionary<string, DiagnosticMethodSubscription> Subscriptions { get; }//TracingDiagnosticMethod
        }
    }



}