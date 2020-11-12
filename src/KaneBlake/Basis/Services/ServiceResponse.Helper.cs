using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Basis.Services
{
    /// <summary>
    /// ServiceResponse 静态方法, 用于生成 ServiceResponse 实例
    /// </summary>
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
                Message = "service request sucess."
            };
            return serviceResponse;
        }

        /// <summary>
        /// Code = 3002: 重定向
        /// </summary>
        /// <param name="Url"></param>
        /// <returns></returns>
        public static ServiceResponse Redirect(string Url)
        {
            var serviceResponse = new ServiceLocationResponse
            {
                StatusCode = 3002,
                Location = Url,
                Message = "3002 Found"
            };
            return serviceResponse;
        }


        /// <summary>
        /// 状态码: 5000 内部异常
        /// </summary>
        /// <returns></returns>
        public static ServiceResponse InnerException()
        {
            var serviceResponse = new ServiceResponse
            {
                StatusCode = 5000,
                Message = "InnerException occurs,please try it later."
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
        public static ServiceResponse BadRequest(IDictionary<string, string[]> modelState)
        {
            var serviceResponse = new ServiceProblemResponse<IDictionary<string, string[]>>
            {
                StatusCode = 4000,
                Errors = modelState,
                Message = "错误的请求信息:参数检验失败."
            };
            return serviceResponse;
        }
        #endregion
    }

    /// <summary>
    /// ServiceResponse 扩展方法
    /// </summary>
    public static class ServiceResponseExtensions 
    {
        /// <summary>
        /// 设置 traceId 的值
        /// </summary>
        /// <param name="serviceResponse"></param>
        /// <param name="traceId"></param>
        public static bool TryAddTraceId(this ServiceResponse serviceResponse, string traceId)
        {
            return serviceResponse.Extensions.TryAdd("traceId", traceId);
        }

        /// <summary>
        /// 判断扩展字段中是否包含 traceId
        /// </summary>
        /// <param name="serviceResponse"></param>
        /// <param name=""></param>
        /// <returns></returns>
        public static bool ContainsTraceId(this ServiceResponse serviceResponse)
        {
            return serviceResponse.Extensions.ContainsKey("traceId");
        }
    }
}
