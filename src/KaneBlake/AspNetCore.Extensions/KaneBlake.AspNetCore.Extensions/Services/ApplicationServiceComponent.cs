using KaneBlake.AspNetCore.Extensions.DependencyInjection;
using KaneBlake.Basis.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Services
{
    /// <summary>
    /// 应用服务上下文(无参数、无返回值)
    /// </summary>
    public interface IApplicationServiceContext
    {
        /// <summary>
        /// 用于组件间通信的字典
        /// 使用组件名称作为 key
        /// </summary>
        public IDictionary<object, object> Items { get; }

        public void AddOrUpdateComponentCahce(IApplicationServiceComponent component, object value)
        {
            Items[component.Name] = value;
        }
    }


    /// <summary>
    /// 应用服务上下文(有参数、无返回值)
    /// </summary>
    /// <typeparam name="TParameter">参数类型</typeparam>
    public interface IApplicationServiceContext<out TParameter> : IApplicationServiceContext
    {
        /// <summary>
        /// 服务请求参数 或 组件间通信的参数
        /// </summary>
        public TParameter Parameter { get; }
    }

    /// <summary>
    /// 应用服务上下文(无参数、有返回值)
    /// </summary>
    /// <typeparam name="TReturnValue">返回值类型</typeparam>
    public interface IApplicationServiceParameterlessContext<out TReturnValue> : IApplicationServiceContext
    {
        /// <summary>
        /// 服务请求返回值
        /// </summary>
        public TReturnValue ReturnValue { get; }
    }

    /// <summary>
    /// 应用服务上下文(有参数、有返回值)
    /// </summary>
    /// <typeparam name="TParameter">参数类型</typeparam>
    /// <typeparam name="TReturnValue">返回值类型</typeparam>
    public interface IApplicationServiceContext<out TParameter, out TReturnValue> : IApplicationServiceContext<TParameter>, IApplicationServiceParameterlessContext<TReturnValue>
    {
    }


    [AutoInjection(ServiceLifetime.Scoped, typeof(IApplicationServiceComponent))]
    /// <summary>
    /// 应用服务组件接口
    /// </summary>
    public interface IApplicationServiceComponent
    {
        /// <summary>
        /// 组件名称
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
        /// 验证上下文类型, 验证成功则执行组件逻辑, 否则返回失败消息.
        /// </summary>
        /// <param name="context">请求上下文</param>
        /// <returns></returns>
        Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context);
    }

    /// <summary>
    /// 应用服务组件抽象类
    /// <para>组件依赖通过构造函数注入</para>
    /// </summary>
    public abstract class ApplicationServiceComponent : IApplicationServiceComponent
    {
        public abstract Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context);
    }


    /// <summary>
    /// 处理泛型上下文的应用服务组件抽象类
    /// <para>组件依赖通过构造函数注入</para>
    /// </summary>
    /// <typeparam name="TServiceContext">上下文类型</typeparam>
    public abstract class ApplicationServiceComponent<TServiceContext> : ApplicationServiceComponent where TServiceContext : IApplicationServiceContext
    {
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
        /// 执行组件逻辑
        /// </summary>
        /// <param name="serviceContext">强类型上下文</param>
        /// <returns></returns>
        public abstract Task<ServiceResponse> InvokeAsync(TServiceContext serviceContext);
    }
}

