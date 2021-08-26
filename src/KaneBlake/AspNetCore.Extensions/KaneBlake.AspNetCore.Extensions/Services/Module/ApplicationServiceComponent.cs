using KaneBlake.AspNetCore.Extensions.DependencyInjection;
using KaneBlake.Basis.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Services.Module
{


    /// <summary>
    /// Defines an interface that represents a application service component to handle <see cref="IApplicationServiceContext"/>.
    /// </summary>
    [AutoInjection(ServiceLifetime.Scoped, typeof(IApplicationServiceComponent))]
    public interface IApplicationServiceComponent
    {
        /// <summary>
        /// The name of component
        /// </summary>
        public string Name => GetType().FullName;


        public Type GenericTypeDefinition
        {
            get
            {
                var instanceType = GetType();
                if (instanceType.IsGenericType)
                {
                    return instanceType.GetGenericTypeDefinition();
                }
                return null;
            }
        }


        /// <summary>
        /// Called to execute the component. 
        /// </summary>
        /// <param name="context">application service context</param>
        /// <returns></returns>
        Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context);
    }

    /// <summary>
    /// Defines an abstraction for application service component without parameter and return value
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    public abstract class ApplicationServiceComponent : IApplicationServiceComponent
    {
        public abstract Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context);
    }


    /// <summary>
    /// Defines an abstraction for application service component with specified <see cref="IApplicationServiceContext"/>
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    /// <typeparam name="TServiceContext">The type of context.</typeparam>
    public abstract class ApplicationServiceComponent<TServiceContext> : ApplicationServiceComponent where TServiceContext : IApplicationServiceContext
    {
        /// <summary>
        /// Execute the component if the type of <paramref name="context"/> is compatible with <typeparamref name="TServiceContext"/>
        /// , otherwise do nothing 
        /// </summary>
        /// <param name="context">Application service context</param>
        /// <returns>Component executed result</returns>
        public override async Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context)
        {
            if (context is TServiceContext serviceContext)
            {
                return await InvokeAsync(serviceContext);
            }

            var component = (IApplicationServiceComponent)this;

            return ServiceResponse.Forbid($"组件 '{component.Name}' 执行失败: 应用服务上下文类型必须实现 {typeof(TServiceContext).FullName} 接口," +
                $"请检查服务配置中的参数类型和返回值类型.");
        }

        /// <summary>
        /// Called to execute the component that handle <paramref name="serviceContext"/>.
        /// </summary>
        /// <param name="serviceContext">Instance of <typeparamref name="TServiceContext"/></param>
        /// <returns></returns>
        public abstract Task<ServiceResponse> InvokeAsync(TServiceContext serviceContext);
    }
}


