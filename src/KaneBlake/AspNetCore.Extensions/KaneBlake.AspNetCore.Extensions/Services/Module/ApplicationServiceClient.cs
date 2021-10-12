using KaneBlake.Basis.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using KaneBlake.Basis.Common.Extensions;
using System.Reflection;
using System.ComponentModel;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

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

        private readonly JsonOptions _jsonOptions;

        private readonly ILogger<ApplicationServiceClient> _logger;

        public ApplicationServiceClient(ApplicationServiceCacheEntryResolver serviceEntryResolver, 
            IEnumerable<IApplicationServiceComponent> components,
            IOptions<JsonOptions> jsonOptions,
            ILogger<ApplicationServiceClient> logger)
        {
            _serviceEntryResolver = serviceEntryResolver ?? throw new ArgumentNullException(nameof(serviceEntryResolver));
            _components = components ?? throw new ArgumentNullException(nameof(components));
            _jsonOptions = jsonOptions?.Value ?? throw new ArgumentNullException(nameof(jsonOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        //var validationResults = new List<ValidationResult>();
        //var validationContext = new ValidationContext(parameter, request.HttpContext?.RequestServices, null);

        //        if (!Validator.TryValidateObject(parameter, validationContext, validationResults, true))
        //        {
        //            return ServiceResponse.BadRequest(ConvertValidationResults(validationResults));
        //        }

        public async Task<ServiceResponse> InvokeAsync(string serviceName, HttpRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            ServiceRequest serviceRequest = null;
            try
            {
                request.Body.Position = 0;
                // var serviceRequest = await request.ReadFromJsonAsync<ServiceRequest>(serviceEntry.ParameterType);
                var body = await request.BodyReader.ReadToEndAsync();
                serviceRequest = JsonSerializer.Deserialize<ServiceRequest>(body.Span, _jsonOptions.JsonSerializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "应用服务 '{serviceName}' 执行失败: 反序列化请求体失败.", serviceName);
                return ServiceResponse.Forbid($"应用服务 '{serviceName}' 执行失败: 请求体格式异常.");
            }
            finally
            {
                request.Body.Position = 0;
            }

            var serviceEntry = await _serviceEntryResolver.ResolveAsync(serviceName);

            if (serviceEntry is null)
            {
                return ServiceResponse.Forbid($"应用服务 '{serviceName}' 解析失败, 请检查配置文件.");
            }

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

            var innerContext = new ApplicationServiceInnerContext(serviceRequest.Body);
            // Todo: 事务管理 or 仓储拦截
            for (var i = 0; i < components.Count; i++)
            {
                var componentResponse = await components[i].InvokeAsync(innerContext);

                if (!componentResponse.OKStatus)
                {
                    return componentResponse;
                }
            }

            if (innerContext.ResponseStore.Count > 0)
            {
                return ServiceResponse.OK(innerContext.ResponseStore);
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


        private class ServiceRequest
        {
            [JsonExtensionData]
            public IDictionary<string, object> Body { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }
    }




}
