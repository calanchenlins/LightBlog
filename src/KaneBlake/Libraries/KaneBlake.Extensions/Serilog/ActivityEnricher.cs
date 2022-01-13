using KaneBlake.Basis.Common.Extensions;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.Extensions.Serilog
{
    /// <summary>
    /// Enriches log events with <see cref="Activity.Current"/>
    /// </summary>
    /// <remarks>
    /// https://github.com/serilog/serilog-aspnetcore/issues/207
    /// https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging/src/LoggerFactoryScopeProvider.cs
    /// </remarks>
    public class ActivityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            ArgumentNullException.ThrowIfNull(logEvent);

            var activity = Activity.Current;

            if (activity is null)
            {
                return;
            }

            const string propertyKey = "__SerilogActivity__";

            var property = activity.GetCustomProperty(propertyKey);

            if (property is not LogEventProperty[] logEventProperties)
            {
                logEventProperties = new LogEventProperty[]
                {
                        propertyFactory.CreateProperty("SpanId", activity.GetSpanId()),
                        propertyFactory.CreateProperty("TraceId", activity.GetTraceId()),
                        propertyFactory.CreateProperty("ParentId", activity.GetParentId())
                };
                activity.SetCustomProperty(propertyKey, logEventProperties);
            }

            foreach (var logEventProperty in logEventProperties)
            {
                logEvent.AddPropertyIfAbsent(logEventProperty);
            }
        }


    }

}
