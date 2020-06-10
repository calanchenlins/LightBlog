using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CoreWeb.Util.Services
{
    public class ServiceHelpOld<T>
    {
        public static ServiceResponseOld<T> SetSuccessResponse(T res)
        {
            var response = new ServiceResponseOld<T>();
            return response.SetSuccessResponse(res);
        }

        public static ServiceResponseOld<T> SetBadResponse()
        {
            var response = new ServiceResponseOld<T>();
            return response.SetBadResponse();
        }
    }
    public class ServiceResponseOld<T>
    {
        public bool Success { get; set; } = false;

        public T Response { get; set; }

        public ServiceResponseOld<T> SetSuccessResponse(T response)
        {
            Success = true;
            Response = response;
            return this;
        }

        public ServiceResponseOld<T> SetBadResponse()
        {
            Success = false;
            return this;
        }
    }
    public class ServiceResponse<T>
    {
        /// <summary>
        /// 服务状态码
        /// 2000: 正常响应
        /// 4001: 参数错误 4003: 拒绝处理 
        /// 5000: 内部异常 
        /// 302:重定向
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// 状态码: 2000 响应数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 状态码: 2000 之外的错误消息
        /// </summary>
        public string Message { get; set; }

        public ServiceResponse<T> SetOkResponse(T response)
        {
            Code = 2000;
            Data = response;
            return this;
        }

        /// <summary>
        /// 状态码: 5000 内部异常 日志记录
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public ServiceResponse<T> SetInnerException(Exception exception)
        {
            // 需要使用日志记录内部错误 exception
            Code = 5000;
            Message = "内部错误, 请稍后重试! ";
            Message = exception.Message;
            return this;
        }

        #region 除正常响应、内部异常之外的响应: 操作不允许、未授权、资源不存在
        #endregion
        public ServiceResponse<T> SetForbiddenResponse(string message)
        {
            Code = 4003;
            Message = message;
            return this;
        }
    }
    public class ServiceHelp<T>
    {
        public static string SetOkResponse(T res)
        {
            var response = new ServiceResponse<T>();
            return JsonConvert.SerializeObject(response.SetOkResponse(res));
        }

        public static string SetInnerException(Exception exception)
        {
            var response = new ServiceResponse<T>();
            return JsonConvert.SerializeObject(response.SetInnerException(exception));
        }
        public static string SetForbiddenResponse(string message)
        {
            var response = new ServiceResponse<T>();
            return JsonConvert.SerializeObject(response.SetForbiddenResponse(message));
        }
    }

}
