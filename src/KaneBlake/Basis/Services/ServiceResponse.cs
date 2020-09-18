using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreWeb.Util.Services
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
        /// 状态码: 2000/4000 之外的错误消息
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
    /// 服务响应状态码
    /// </summary>
    public static class ServiceStatusCodes
    {
        /// <summary>
        /// 正常响应状态码
        /// </summary>
        public const int Status2000OK = 2000;
    }

    public partial class ServiceResponse 
    {
        /// <summary>
        /// Code = 2000: 正常响应
        /// </summary>
        /// <returns></returns>
        public static ServiceResponse OK()
        {
            var serviceResponse = new ServiceResponse
            {
                StatusCode = 2000,
                Message = "action execute sucess."
            };
            return serviceResponse;
        }

        /// <summary>
        /// Code = 302: 重定向
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static ServiceResponse Redirect(string Url)
        {
            var serviceResponse = new ServiceResponse
            {
                StatusCode = 3002,
                Message = Url
            };
            return serviceResponse;
        }

        /// <summary>
        /// 状态码: 5000 内部异常 日志记录
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static ServiceResponse InnerException(Exception exception)
        {
            // 需要使用日志记录内部错误 exception
            var serviceResponse = new ServiceResponse
            {
                StatusCode = 5000,
                Message = "内部错误, 请稍后重试! "
            };
            return serviceResponse;
        }

        /// <summary>
        /// 状态码: 4003 禁止操作
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static ServiceResponse Forbid(string message)
        {
            var serviceResponse = new ServiceResponse
            {
                StatusCode = 4003,
                Message = message
            };
            return serviceResponse;
        }

        /// <summary>
        /// 状态码: 4001 未授权
        /// </summary>
        /// <returns></returns>
        public static ServiceResponse Unauthorized()
        {
            var serviceResponse = new ServiceResponse
            {
                StatusCode = 4001,
                Message = "Unauthorized."
            };
            return serviceResponse;
        }

        #region T Data 泛型方法
        /// <summary>
        /// Code = 2000: 正常响应
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ServiceResponse<T> OK<T>(T data)
        {
            var serviceResponse = new ServiceResponse<T>
            {
                StatusCode = 2000,
                Result = data
            };
            return serviceResponse;
        }

        /// <summary>
        /// 状态码: 4000 错误的请求信息:参数检验失败等等
        /// </summary>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public static ServiceResponse BadRequest(Dictionary<string, object> modelState)
        {
            var serviceResponse = new ServiceResponse<Dictionary<string, object>>
            {
                StatusCode = 4000,
                Result = modelState,
                Message = "错误的请求信息:参数检验失败."
            };
            return serviceResponse;
        }

        /// <summary>
        /// 状态码: 4000 错误的请求信息:参数检验失败等等
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ServiceResponse BadRequest<T>(T data)
        {
            var serviceResponse = new ServiceResponse<T>
            {
                StatusCode = 4000,
                Result = data,
                Message = "错误的请求信息:参数检验失败."
            };
            return serviceResponse;
        }
        #endregion
    }
}
