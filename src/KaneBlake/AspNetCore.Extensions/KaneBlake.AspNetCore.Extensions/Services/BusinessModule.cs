using KaneBlake.Basis.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Services
{
    // 责任链+组合模式
    class BusinessModule
    {
    }
    public delegate Task<ServiceResponse> RequestDelegate(object context);

    public class BusinessServiceProvider
    {
        public string ServiceName { get; set; }
    }

    public class ServiceBuilder
    {
        private readonly List<Func<RequestDelegate, RequestDelegate>> _components = new List<Func<RequestDelegate, RequestDelegate>>();

        public ServiceBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            _components.Add(middleware);
            return this;
        }
        public RequestDelegate Build()
        {
            RequestDelegate app = async context =>
            {
                // If we reach the end of the pipeline
                await Task.CompletedTask;
                return ServiceResponse.OK();
            };

            for (var c = _components.Count - 1; c >= 0; c--)
            {
                app = _components[c](app);
            }

            return app;
        }
    }



    public interface ICommonSaveComponent
    {


        Task<ServiceResponse> InvokeAsync(object context, RequestDelegate next);
    }

    public class CommonSaveComponent : ICommonSaveComponent
    {
        // 组件不知道调用者是谁
        // 组件不知道被调用的顺序
        // 组件可以随时返回任何格式的消息
        // 

        public async Task<ServiceResponse> InvokeAsync(object context, RequestDelegate next)
        {
            // 验证

            // 业务操作

            return await next(context);

        }
    }


    public interface ICGBGService
    {
        ServiceResponse Delete();
        ServiceResponse Save();
        ServiceResponse Submit();
    }
    public class CGBGService : ICGBGService
    {

        // 不同请求参数(租户)不同实现
        public ServiceResponse Save()
        {
            // 参数验证

            // 业务逻辑

            // 数据交付

            return ServiceResponse.OK();
        }


        public ServiceResponse Delete()
        {
            return ServiceResponse.OK();
        }

        public ServiceResponse Submit()
        {
            return ServiceResponse.OK();
        }

    }
}
