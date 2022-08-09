using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.Services.Module
{
    /// <summary>
    /// Implements policy for resolving <see cref="ApplicationServiceCacheEntry"/> from <see cref="ApplicationServiceOptions"/>.
    /// used for  
    /// </summary>
    public class ApplicationServiceCacheEntryResolver
    {
        private readonly IOptionsMonitor<ApplicationServiceOptions> _optionsMonitor;

        private readonly ILogger<ApplicationServiceCacheEntryResolver> _logger;

        private readonly ConcurrentDictionary<string, ApplicationServiceCacheEntry> _serviceEntries;

        public ApplicationServiceCacheEntryResolver(IOptionsMonitor<ApplicationServiceOptions> optionsMonitor, ILogger<ApplicationServiceCacheEntryResolver> logger)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceEntries = new ConcurrentDictionary<string, ApplicationServiceCacheEntry>();

            _optionsMonitor.OnChange((options, name) =>
            {
                _serviceEntries.Clear();
            });
        }

        /// <summary>
        /// resolving <see cref="ApplicationServiceCacheEntry"/> by name and cache it.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        internal async Task<ApplicationServiceCacheEntry> ResolveAsync(string serviceName)
        {
            await Task.CompletedTask;

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (_serviceEntries.TryGetValue(serviceName, out var serviceEntry))
            {
                return serviceEntry;
            }

            serviceEntry = Resolve(serviceName);

            _serviceEntries.TryAdd(serviceName, serviceEntry);

            return serviceEntry;
        }

        private ApplicationServiceCacheEntry Resolve(string serviceName) 
        {
            var options = _optionsMonitor.CurrentValue;

            if (!options.Services.TryGetValue(serviceName, out var serviceConfiguration))
            {
                _logger.LogWarning("ApplicationService '{serviceName}' was not configured.", serviceName);
                return null;
            }

            if ((serviceConfiguration.Components?.Count ?? 0) <= 0)
            {
                _logger.LogWarning("Failed to resolve applicationService '{serviceName}': The components section was not configured.", serviceName);
                return null;
            }


            var orderedComponentNames = serviceConfiguration.Components
                .Where(c=>!string.IsNullOrWhiteSpace(c.Name))
                .OrderBy(c => c.Order)
                .Select(c => c.Name.Trim())
                .ToArray();

            return new ApplicationServiceCacheEntry(orderedComponentNames);
        }

        private Type GetType(string serviceName, string typeName)
        {
            Type type;
            try
            {
                type = Type.GetType(typeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve applicationService '{serviceName}': An error occurred while getting the System.Type with the specified name '{typeName}'.", serviceName, typeName);
                return null;
            }

            if (type is null)
            {
                _logger.LogWarning("Failed to resolve applicationService '{serviceName}': System.Type with the specified name '{typeName}' was not found in the currently executing assembly or in Mscorlib.dll.", serviceName, typeName);
            }
            return type;
        }


    }

    /// <summary>
    /// Represent a cache entry of application service
    /// </summary>
    internal class ApplicationServiceCacheEntry
    {
        public string[] OrderedComponentNames { get; set; }

        public ApplicationServiceCacheEntry(string[] orderedComponentNames)
        {
            OrderedComponentNames = orderedComponentNames ?? throw new ArgumentNullException(nameof(orderedComponentNames));
        }
    }

}
