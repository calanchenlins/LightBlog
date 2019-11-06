using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Services
{
    public class ServiceResponse<T>
    {
        public bool Success { get; set; } = false;

        public T Response { get; set; } 

        public ServiceResponse<T> SetSuccessResponse(T response)
        {
            Success = true;
            Response = response;
            return this;
        }

        public ServiceResponse<T> SetBadResponse()
        {
            Success = false;
            return this;
        }
    }
    public class ServiceHelp<T>
    {
        public static ServiceResponse<T> SetSuccessResponse(T res)
        {
            var response = new ServiceResponse<T>();
            return response.SetSuccessResponse(res);
        }

        public static ServiceResponse<T> SetBadResponse()
        {
            var response = new ServiceResponse<T>();
            return response.SetBadResponse();
        }
    }
}
