using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWeb.Util.Services
{
    /// <summary>
    /// Service 层响应格式
    /// </summary>
    public class ServiceResponse 
    {
        /// <summary>
        /// 服务接口响应状态码
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 状态码: 2000/4000 之外的错误消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 服务接口是否正常响应
        /// </summary>
        public bool OKStatus => Code.Equals(ServiceStatusCodes.Status2000OK);

        /// <summary>
        /// Code = 2000: 正常响应
        /// </summary>
        /// <returns></returns>
        public static ServiceResponse OK()
        {
            var serviceResponse = new ServiceResponse
            {
                Code = 2000,
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
                Code = 3002,
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
                Code = 5000,
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
                Code = 4003,
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
                Code = 4001,
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
                Code = 2000,
                Data = data
            };
            return serviceResponse;
        }

        /// <summary>
        /// 状态码: 4000 错误的请求信息:参数检验失败等等
        /// </summary>
        /// <param name="moelState"></param>
        /// <returns></returns>
        public static ServiceResponse<T> BadRequest<T>(T moelState)
        {
            var serviceResponse = new ServiceResponse<T>
            {
                Code = 4000,
                Data = moelState
            };
            return serviceResponse;
        }
        #endregion
    }

    /// <summary>
    /// 包含泛型 Data 的 Service 层响应格式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ServiceResponse<T>: ServiceResponse
    {

        /// <summary>
        /// 状态码: 2000 响应数据/状态码: 3002 重定向Url
        /// </summary>
        public T Data { get; set; }
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
