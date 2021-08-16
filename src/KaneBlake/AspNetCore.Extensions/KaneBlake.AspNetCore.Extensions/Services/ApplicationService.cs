using KaneBlake.Basis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Services
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
    /// 应用服务配置
    /// </summary>
    public class ApplicationServiceOptions
    {
        public Dictionary<string, ServiceConfiguration> Services { get; set; }

        public class ServiceConfiguration
        {
            /// <summary>
            /// 请求参数类型
            /// </summary>
            public string Parameter { get; set; }

            /// <summary>
            /// 返回值类型
            /// </summary>
            public string ReturnValue { get; set; }

            /// <summary>
            /// 组件
            /// </summary>
            public List<ComponentConfiguration> Components { get; set; }
        }

        public class ComponentConfiguration
        {
            /// <summary>
            /// 组件顺序
            /// </summary>
            public int Order { get; set; } = -1;

            /// <summary>
            /// 组件名称
            /// </summary>
            public string Name { get; set; }
        }
    }

    /// <summary>
    /// 单例
    /// </summary>
    public class ApplicationServiceEntryResolver
    {
        private readonly IOptionsMonitor<ApplicationServiceOptions> _optionsMonitor;

        private readonly ILogger<ApplicationServiceEntryResolver> _logger;

        private readonly ConcurrentDictionary<string, ApplicationServiceEntry> _serviceEntries;

        public ApplicationServiceEntryResolver(IOptionsMonitor<ApplicationServiceOptions> optionsMonitor, ILogger<ApplicationServiceEntryResolver> logger)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceEntries = new ConcurrentDictionary<string, ApplicationServiceEntry>();

            _optionsMonitor.OnChange((options, name) =>
            {
                _serviceEntries.Clear();
            });
        }

        internal async Task<ApplicationServiceEntry> ResolveAsync(string serviceName)
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

            serviceEntry = new ApplicationServiceEntry();

            var options = _optionsMonitor.CurrentValue;

            if (!options.Services.TryGetValue(serviceName, out var serviceConfiguration))
            {
                _logger.LogWarning("ApplicationService '{serviceName}' was not configured.", serviceName);
                return null;
            }

            if (string.IsNullOrWhiteSpace(serviceConfiguration.Parameter) || (serviceConfiguration.Components?.Count ?? 0) <= 0)
            {
                _logger.LogWarning("Failed to resolve applicationService '{serviceName}': The Parameter or Components was not configured.", serviceName);
                return null;
            }

            if ((serviceEntry.ParameterType = GetType(serviceName, serviceConfiguration.Parameter)) is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(serviceConfiguration.ReturnValue) &&
                (serviceEntry.ReturnValueType = GetType(serviceName, serviceConfiguration.ReturnValue)) is null
                )
            {
                return null;
            }


            try
            {
                serviceEntry.ContextType = (serviceEntry.ParameterType, serviceEntry.ReturnValueType) switch
                {
                    (Type parameterType, null) => typeof(ApplicationServiceContext<>).MakeGenericType(serviceEntry.ParameterType),
                    (null, Type returnValueType) => typeof(ApplicationServiceParameterlessContext<>).MakeGenericType(serviceEntry.ReturnValueType),
                    (Type parameterType, Type returnValueType) => typeof(ApplicationServiceContext<,>).MakeGenericType(serviceEntry.ParameterType, serviceEntry.ReturnValueType),
                    (_, _) => typeof(ApplicationServiceContext),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve applicationService '{serviceName}': " +
                    "An error occurred while constructing an generic ApplicationServiceContext type with the specified typeArguments ['{typeArguments}']."
                    , serviceName, new string[] { serviceEntry.ParameterType.FullName, serviceEntry.ReturnValueType?.FullName });
                return null;
            }

            serviceEntry.OrderedComponentNames = serviceConfiguration.Components.OrderBy(c => c.Order).Select(c => c.Name).ToList();

            _serviceEntries.TryAdd(serviceName, serviceEntry);

            return serviceEntry;
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
                _logger.LogError(ex, "Failed to resolve applicationService '{serviceName}': An error occurred while getting the System.Type with the specified name '{ParameterType}'.", serviceName, typeName);
                return null;
            }

            if (type is null)
            {
                _logger.LogWarning("Failed to resolve applicationService '{serviceName}': System.Type with the specified name '{ParameterType}' was not found in the currently executing assembly or in Mscorlib.dll.", serviceName, typeName);
            }
            return type;
        }


        internal class ApplicationServiceEntry
        {
            /// <summary>
            /// 服务参数类型
            /// </summary>
            public Type ParameterType { get; set; }


            public Type ReturnValueType { get; set; }

            public Type ContextType { get; set; }

            public List<string> OrderedComponentNames { get; set; }
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


    /// <summary>
    /// Scoped ApplicationServiceClient
    /// </summary>
    public class ApplicationService
    {
        private readonly ApplicationServiceEntryResolver _serviceEntryResolver;

        private readonly IEnumerable<IApplicationServiceComponent> _components;

        private readonly ILogger<ApplicationService> _logger;

        public ApplicationService(ApplicationServiceEntryResolver serviceEntryResolver, IEnumerable<IApplicationServiceComponent> components, ILogger<ApplicationService> logger)
        {
            _serviceEntryResolver = serviceEntryResolver ?? throw new ArgumentNullException(nameof(serviceEntryResolver));
            _components = components ?? throw new ArgumentNullException(nameof(components));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResponse> InvokeAsync(string serviceName, HttpRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var serviceEntry = await _serviceEntryResolver.ResolveAsync(serviceName);

            if (serviceEntry is null)
            {
                return ServiceResponse.Forbid($"应用服务 '{serviceName}' 解析失败, 请检查配置文件.");
            }

            object parameter = null;
            object defaultReturnValue = null;

            if (!(serviceEntry.ParameterType is null)) 
            {
                try
                {
                    request.Body.Position = 0;

                    //parameter = await request.ReadFromJsonAsync(serviceEntry.ParameterType);

                    var validationResults = new List<ValidationResult>();

                    var validationContext = new ValidationContext(parameter, request.HttpContext?.RequestServices, null);

                    if (!Validator.TryValidateObject(parameter, validationContext, validationResults, true))
                    {
                        return ServiceResponse.BadRequest(ConvertValidationResults(validationResults));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "应用服务 '{serviceName}' 执行异常: 参数类型 '{ParameterType}' 绑定失败", serviceName, serviceEntry.ParameterType.FullName);
                    return ServiceResponse.Forbid($"应用服务 '{serviceName}' 执行异常: 参数类型 '{serviceEntry.ParameterType.FullName}' 绑定失败");
                }
                finally
                {
                    request.Body.Position = 0;
                }
            }
            if (!(serviceEntry.ReturnValueType is null)) 
            {
                try 
                {
                    defaultReturnValue = Activator.CreateInstance(serviceEntry.ReturnValueType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "应用服务 '{serviceName}' 执行异常: 创建 '{ParameterType}' 类型实例失败, 服务返回类型必须拥有公共无参构造函数."
                        , serviceName, serviceEntry.ReturnValueType.FullName);
                    return ServiceResponse.Forbid($"应用服务 '{serviceName}' 执行异常: 创建 '{serviceEntry.ReturnValueType.FullName}' 类型实例失败," +
                        $"\r\n服务返回类型必须拥有公共无参构造函数.");
                }

            }

            var context = (serviceEntry.ParameterType, serviceEntry.ReturnValueType) switch
            {
                (Type parameterType, null) => Activator.CreateInstance(serviceEntry.ContextType, parameter) ,
                (null, Type returnValueType) => Activator.CreateInstance(serviceEntry.ContextType, defaultReturnValue),
                (Type parameterType, Type returnValueType) => Activator.CreateInstance(serviceEntry.ContextType, parameter, defaultReturnValue),
                (_, _) => Activator.CreateInstance(serviceEntry.ContextType)
            } as IApplicationServiceContext;


            var componentNames = serviceEntry.OrderedComponentNames;

            var components = new List<IApplicationServiceComponent>(componentNames.Count);

            for (var i = 0; i < componentNames.Count; i++)
            {
                var component = _components.FirstOrDefault(c => c.Name.Equals(componentNames[i]));

                if (component is null)
                {
                    return ServiceResponse.Forbid($"应用服务 '{serviceName}' 解析失败: 组件'{componentNames[i]}' 未在 DI 容器中注册.");
                }
                components.Add(component);
            }

            // Todo: 事务管理 or 仓储拦截
            for (var i = 0; i < components.Count; i++)
            {
                var componentResponse = await components[i].InvokeAsync(context);

                if (!componentResponse.OKStatus)
                {
                    return componentResponse;
                }
            }

            if (context is IApplicationServiceContextReturnValue serviceContext)
            {
                return ServiceResponse.OK(serviceContext.ReturnValue);
            }

            return ServiceResponse.OK();
        }

        private static IDictionary<string, string[]> ConvertValidationResults(IEnumerable<ValidationResult> results)
        {
            static IEnumerable<(string MemberName, string ErrorMessage)> ExpandValidationResults(IEnumerable<ValidationResult> results)
            {
                foreach (var result in results)
                {
                    if (result != ValidationResult.Success)
                    {
                        if (result.MemberNames is null || !result.MemberNames.Any())
                        {
                            yield return (string.Empty, result.ErrorMessage);
                        }
                        else
                        {
                            foreach (var memberName in result.MemberNames)
                            {
                                yield return (memberName, result.ErrorMessage);
                            }
                        }
                    }
                }
            }
            var collection = ExpandValidationResults(results)
                        .GroupBy(r => r.MemberName)
                        .Select(g => new KeyValuePair<string, string[]>(g.Key, g.Select(r => r.ErrorMessage).ToArray()));

            return new Dictionary<string, string[]>(collection);
        }


    }
}

