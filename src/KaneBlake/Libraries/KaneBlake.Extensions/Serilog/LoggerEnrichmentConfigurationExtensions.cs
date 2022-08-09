using Serilog;
using Serilog.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace K.Extensions.Serilog
{
    public static class LoggerEnrichmentConfigurationExtensions
    {
        public static LoggerConfiguration WithMachineIp(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration.With<MachineIpEnricher>();
        }
        public static LoggerConfiguration WithActivity(this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            return enrichmentConfiguration.With<ActivityEnricher>();
        }
    }
}
