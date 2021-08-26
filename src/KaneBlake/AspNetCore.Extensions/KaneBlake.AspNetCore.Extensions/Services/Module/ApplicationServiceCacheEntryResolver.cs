using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Services.Module
{
    internal interface IApplicationServiceContextParameter
    {
        public object Parameter { get; }
    }

    internal interface IApplicationServiceContextReturnValue
    {
        public object ReturnValue { get; }
    }



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

            
            Type parameterType = null;
            Type returnValueType = null;

            if (!string.IsNullOrWhiteSpace(serviceConfiguration.Parameter) &&
                (parameterType = GetType(serviceName, serviceConfiguration.Parameter)) is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(serviceConfiguration.ReturnValue) &&
                (returnValueType = GetType(serviceName, serviceConfiguration.ReturnValue)) is null
                )
            {
                return null;
            }

            var orderedComponentNames = serviceConfiguration.Components
                .Where(c=>!string.IsNullOrWhiteSpace(c.Name))
                .OrderBy(c => c.Order)
                .Select(c => c.Name.Trim())
                .ToArray();

            return new ApplicationServiceCacheEntry(parameterType, returnValueType, orderedComponentNames);
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
        /// <summary>
        /// The type of application service request parameter
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// The type of application service returnValue
        /// </summary>
        public Type ReturnValueType { get; }

        public string[] OrderedComponentNames { get; set; }

        private Type _contextType;

        public ApplicationServiceCacheEntry(Type parameterType, Type returnValueType, string[] orderedComponentNames)
        {
            ParameterType = parameterType;
            ReturnValueType = returnValueType;
            OrderedComponentNames = orderedComponentNames ?? throw new ArgumentNullException(nameof(orderedComponentNames));
        }

        /// <summary>
        /// Create instances of an <see cref="IApplicationServiceContext"/>
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="defaultReturnValue"></param>
        /// <returns></returns>
        public IApplicationServiceContext CreateContext(object parameter, object defaultReturnValue)
        {
            if (_contextType == null)
            {
                _contextType = (ParameterType, ReturnValueType) switch
                {
                    (Type parameterType, null) => typeof(ApplicationServiceContext<>).MakeGenericType(parameterType),
                    (null, Type returnValueType) => typeof(ApplicationServiceParameterlessContext<>).MakeGenericType(returnValueType),
                    (Type parameterType, Type returnValueType) => typeof(ApplicationServiceContext<,>).MakeGenericType(parameterType, returnValueType),
                    (_, _) => typeof(ApplicationServiceContext),
                };
            }

            var args = (ParameterType, ReturnValueType) switch
            {
                (Type _, null) => new object[] { parameter },
                (null, Type _) => new object[] { defaultReturnValue },
                (Type _, Type _) => new object[] { parameter, defaultReturnValue },
                (_, _) => Array.Empty<object>()
            };

            return Activator.CreateInstance(_contextType, args) as IApplicationServiceContext;
        }


        private class ApplicationServiceContext : IApplicationServiceContext
        {
            public IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();
        }


        private class ApplicationServiceContext<TParameter> : ApplicationServiceContext, IApplicationServiceContext<TParameter>, IApplicationServiceContextParameter
        {
            public ApplicationServiceContext(TParameter parameter)
            {
                Parameter = parameter;
            }

            public TParameter Parameter { get; }

            object IApplicationServiceContextParameter.Parameter => Parameter;
        }


        private class ApplicationServiceParameterlessContext<TReturnValue> : ApplicationServiceContext, IApplicationServiceParameterlessContext<TReturnValue>, IApplicationServiceContextReturnValue
        {
            public ApplicationServiceParameterlessContext(TReturnValue returnValue)
            {
                ReturnValue = returnValue;
            }

            public TReturnValue ReturnValue { get; }

            object IApplicationServiceContextReturnValue.ReturnValue => ReturnValue;
        }


        private class ApplicationServiceContext<TParameter, TReturnValue> : ApplicationServiceContext<TParameter>, IApplicationServiceContext<TParameter, TReturnValue>, IApplicationServiceContextReturnValue
        {
            public ApplicationServiceContext(TParameter parameter, TReturnValue returnValue) : base(parameter)
            {
                ReturnValue = returnValue;
            }

            public TReturnValue ReturnValue { get; }

            object IApplicationServiceContextReturnValue.ReturnValue => ReturnValue;
        }

    }

}
