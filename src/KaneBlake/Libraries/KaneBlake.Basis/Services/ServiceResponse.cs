using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace K.Basis.Services
{
    /// <summary>
    /// Service、WebApi 层响应格式
    /// <code>
    /// <para>请求成功: { statusCode: 2000, result: {} }  <seealso cref="ServiceResponse{T}.Result"/></para>
    /// <para>参数验证失败: { statusCode: 4000, errors: {"modelName": ["modelError1","modelError2"] }} <seealso cref="ServiceProblemResponse{T}.Errors"/></para>
    /// <para>禁止操作: {statusCode: 4003, message: "详细错误消息"} <seealso cref="Message"/></para>
    /// <para>系统错误: {statusCode: 5000, message: "详细错误消息"} <seealso cref="Message"/></para>
    /// </code>
    /// </summary>
    /// <remarks>自定义序列化: src\Mvc\Mvc.Core\src\Infrastructure\ProblemDetailsJsonConverter.cs</remarks>
    public partial class ServiceResponse 
    {
        /// <summary>
        /// 服务接口响应状态码
        /// </summary>
        /// <example>2000</example>
        [JsonPropertyName("statusCode")]
        public int? StatusCode { get; set; }

        /// <summary>
        /// 服务响应消息
        /// </summary>
        /// <example>service request sucess.</example>
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
    /// 服务请求成功响应 
    /// </summary>
    /// <remarks>状态码: 2000</remarks>
    /// <typeparam name="T"></typeparam>
    public class ServiceResponse<T>: ServiceResponse
    {
        public ServiceResponse(T result)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            StatusCode = 2000;
        }

        /// <summary>
        /// 响应数据
        /// </summary>
        [JsonPropertyName("result")]
        public T Result { get; set; }
    }


    /// <summary>
    /// 服务重定向响应
    /// </summary>
    /// <remarks>状态码: 3002</remarks>
    public class ServiceLocationResponse : ServiceResponse
    {
        public ServiceLocationResponse(string location)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            StatusCode = 3002;
            Message = "3002 Found";
        }

        /// <summary>
        /// 重定向地址
        /// </summary>
        [JsonPropertyName("location")]
        public string Location { get; set; }
    }


    /// <summary>
    /// 服务异常响应
    /// </summary>
    /// <remarks>状态码: 4000 等</remarks>
    /// <typeparam name="T"></typeparam>
    public class ServiceProblemResponse<T> : ServiceResponse
    {
        /// <summary>
        /// 异常信息
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
