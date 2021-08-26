using KaneBlake.Basis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Services.Module
{
    /// <summary>
    /// Represents an applicationService client.
    /// </summary>
    public interface IApplicationServiceClient
    {
        /// <summary>
        /// Resolve applicationService by name and execute it.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<ServiceResponse> InvokeAsync(string serviceName, HttpRequest request);
    }

    /// <summary>
    /// The default implementation of <see cref="IApplicationServiceClient"/>
    /// </summary>
    internal class ApplicationServiceClient : IApplicationServiceClient
    {
        private readonly ApplicationServiceCacheEntryResolver _serviceEntryResolver;

        private readonly IEnumerable<IApplicationServiceComponent> _components;

        private readonly ILogger<ApplicationServiceClient> _logger;

        public ApplicationServiceClient(ApplicationServiceCacheEntryResolver serviceEntryResolver, IEnumerable<IApplicationServiceComponent> components, ILogger<ApplicationServiceClient> logger)
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


            var context = serviceEntry.CreateContext(parameter, defaultReturnValue);


            var componentNames = serviceEntry.OrderedComponentNames;

            var components = new List<IApplicationServiceComponent>(componentNames.Length);

            for (var i = 0; i < componentNames.Length; i++)
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
