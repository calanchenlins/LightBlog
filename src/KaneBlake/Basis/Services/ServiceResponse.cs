using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaneBlake.Basis.Services
{
    /// <summary>
    /// Service、WebApi 层响应格式
    /// 自定义序列化: src\Mvc\Mvc.Core\src\Infrastructure\ProblemDetailsJsonConverter.cs
    /// </summary>
    public partial class ServiceResponse 
    {
        /// <summary>
        /// 服务接口响应状态码
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }

        /// <summary>
        /// 状态码: 2000 之外的错误消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets the <see cref="IDictionary{TKey, TValue}"/> for extension members.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        /// 服务接口是否正常响应
        /// </summary>
        [JsonIgnore]
        public bool OKStatus => StatusCode.Equals(ServiceStatusCodes.Status2000OK);
    }

    /// <summary>
    /// 包含泛型 Data 的 Service、WebApi 层响应格式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceResponse<T>: ServiceResponse
    {
        /// <summary>
        /// 状态码: 2000 响应数据/状态码: 3002 重定向Url
        /// </summary>
        [JsonPropertyName("result")]
        public T Result { get; set; }
    }


    /// <summary>
    /// 服务重定向响应 状态码: 3002
    /// </summary>
    public class ServiceLocationResponse : ServiceResponse
    {
        /// <summary>
        /// 重定向地址
        /// </summary>
        [JsonPropertyName("location")]
        public string Location { get; set; }
    }

    /// <summary>
    /// 服务异常响应
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceProblemResponse<T> : ServiceResponse
    {
        /// <summary>
        /// 状态码: 4000 模型验证错误信息
        /// </summary>
        [JsonPropertyName("errors")]
        public T Errors { get; set; }
    }


    /// <summary>
    /// 服务响应状态码
    /// </summary>
    public static class ServiceStatusCodes
    {
        /// <summary>
        /// 正常响应状态码
        /// </summary>
        public const int Status2000OK = 2000;
    }
}
