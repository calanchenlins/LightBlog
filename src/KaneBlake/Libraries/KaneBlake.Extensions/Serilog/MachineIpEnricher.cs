using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace K.Extensions.Serilog
{
    /// <summary>
    /// Enriches log events with a EnvironmentName property containing the value of the ASPNETCORE_ENVIRONMENT or DOTNET_ENVIRONMENT environment variable.
    /// </summary>
    public class MachineIpEnricher : ILogEventEnricher
    {
        private LogEventProperty? _cachedProperty;

        /// <summary>
        /// The property name added to enriched log events.
        /// </summary>
        public const string MachineIpv4AddressPropertyName = "MachineIpv4Address";

        /// <summary>
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Don't care about thread-safety, in the worst case the field gets overwritten and one
            // property will be GCed
            _cachedProperty ??= CreateProperty(propertyFactory);
            logEvent.AddPropertyIfAbsent(_cachedProperty);
        }


        // Qualify as uncommon-path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static LogEventProperty CreateProperty(ILogEventPropertyFactory propertyFactory)
        {
            var Ipv4Address = string.Empty;
            try
            {
                Ipv4Address = NetworkInterface.GetAllNetworkInterfaces()
                           .Where(x => x.OperationalStatus == OperationalStatus.Up)
                           .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                           .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                           .Select(x => x.Address.ToString())
                           .FirstOrDefault();
            }
            catch (Exception) { }

            return propertyFactory.CreateProperty(MachineIpv4AddressPropertyName, Ipv4Address);
        }
    }


}
