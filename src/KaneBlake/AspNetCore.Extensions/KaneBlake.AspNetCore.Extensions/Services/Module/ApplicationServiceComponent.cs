using KaneBlake.AspNetCore.Extensions.DependencyInjection;
using KaneBlake.Basis.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;

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

        /// <summary>
        /// The type of application service context
        /// </summary>
        public Type ContextType => typeof(IApplicationServiceContext);

        /// <summary>
        /// Called to execute the component. 
        /// </summary>
        /// <param name="context">Application service context</param>
        /// <returns>Component executed result</returns>
        Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context);
    }


    /// <summary>
    /// Defines an abstraction for application service component that handle specified <see cref="IApplicationServiceContext"/>
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    /// <typeparam name="TContext">The type of application service component context.</typeparam>
    public abstract class ApplicationServiceComponentBase<TContext> : IApplicationServiceComponent where TContext : IApplicationServiceContext
    {
        public virtual Type ContextType => typeof(TContext);

        /// <summary>
        /// Transform <see cref="ApplicationServiceInnerContext"/> to <typeparamref name="TContext"/>
        /// </summary>
        /// <param name="innerContext">application service context</param>
        /// <returns>application component context</returns>
        internal abstract TContext CreateContext(ApplicationServiceInnerContext innerContext);

        public virtual async Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context)
        {
            if (!(context is ApplicationServiceInnerContext innerContext))
            {
                var component = (IApplicationServiceComponent)this;
                return ServiceResponse.Forbid($"组件 '{component.Name}' 执行失败: " +
                    $"不支持的应用服务上下文类型 '{context.GetType().FullName}'.");
            }
            return await InvokeAsync(CreateContext(innerContext));
        }

        /// <summary>
        /// Called to execute the component that handle specified <paramref name="componentContext"/>.
        /// </summary>
        /// <param name="componentContext">Instance of <typeparamref name="TContext"/></param>
        /// <returns></returns>
        public abstract Task<ServiceResponse> InvokeAsync(TContext componentContext);
    }


    /// <summary>
    /// Defines an abstraction for application service component that handle <see cref="IApplicationServiceContext"/>
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    public abstract class ApplicationServiceComponent : IApplicationServiceComponent
    {
        public abstract Task<ServiceResponse> InvokeAsync(IApplicationServiceContext context);
    }


    /// <summary>
    /// Defines an abstraction for application service component that handle <see cref="IApplicationServiceContext{TParameter}"/>
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter in context.</typeparam>
    public abstract class ApplicationServiceComponent<TParameter> 
        : ApplicationServiceComponentBase<IApplicationServiceContext<TParameter>>
        where TParameter : class
    {
        internal override IApplicationServiceContext<TParameter> CreateContext(ApplicationServiceInnerContext innerContext)
        {
            return new ApplicationServiceContext<TParameter>
            {
                Items = innerContext.Items,
                Parameter = ContractCache<TParameter>.Factory.Create(innerContext.SharedStore),
            };
        }
    }


    /// <summary>
    /// Defines an abstraction for application service component that handle <see cref="IApplicationServiceContextWithReturnValue{TReturnValue}"/>
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    /// <typeparam name="TReturnValue">The type of return value in context.</typeparam>
    public abstract class ApplicationServiceComponentWithReturnValue<TReturnValue>
        : ApplicationServiceComponentBase<IApplicationServiceContextWithReturnValue<TReturnValue>>
        where TReturnValue : class
    {
        internal override IApplicationServiceContextWithReturnValue<TReturnValue> CreateContext(ApplicationServiceInnerContext innerContext)
        {
            return new ApplicationServiceContextWithReturnValue<TReturnValue>
            {
                Items = innerContext.Items,
                ReturnValue = ContractCache<TReturnValue>.Factory.Create(innerContext.ResponseStore),
            };
        }
    }


    /// <summary>
    /// Defines an abstraction for application service component that handle <see cref="IApplicationServiceContext{TParameter, TReturnValue}"/>
    /// <para>Using constructor injection to resolve component's dependency</para>
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter in context.</typeparam>
    /// <typeparam name="TReturnValue">The type of return value in context.</typeparam>
    public abstract class ApplicationServiceComponent<TParameter, TReturnValue>
        : ApplicationServiceComponentBase<IApplicationServiceContext<TParameter, TReturnValue>>
        where TParameter : class
        where TReturnValue : class
    {
        internal override IApplicationServiceContext<TParameter, TReturnValue> CreateContext(ApplicationServiceInnerContext innerContext)
        {
            return new ApplicationServiceContext<TParameter, TReturnValue>
            {
                Items = innerContext.Items,
                Parameter = ContractCache<TParameter>.Factory.Create(innerContext.SharedStore),
                ReturnValue = ContractCache<TReturnValue>.Factory.Create(innerContext.ResponseStore)
            };
        }
    }



    public interface IContractFactory<TMessage>
    {
        TMessage Create(IDictionary<string, object> store);
    }


    public class ContractFactory<TMessage> : IContractFactory<TMessage> where TMessage : class
    {
        private readonly Type _proxyType;

        public ContractFactory(Type proxyType)
        {
            _proxyType = proxyType ?? throw new ArgumentNullException(nameof(proxyType));
        }

        public TMessage Create(IDictionary<string, object> store)
        {
            if (_proxyType is null)
            {
                return default;
            }
            else
            {
                return Activator.CreateInstance(_proxyType, store) as TMessage;
            }
        }
    }
    public class ContractCache<TMessage> where TMessage : class
    {
        private static Lazy<IContractFactory<TMessage>> _factoryCache => new Lazy<IContractFactory<TMessage>>(CreateContractFactory);

        public static IContractFactory<TMessage> Factory => _factoryCache.Value;


        private static IContractFactory<TMessage> CreateContractFactory()
        {
            var contractType = typeof(TMessage);
            var proxyType = contractType.Assembly.GetCustomAttributes<AutoImplementationItemAttribute>()
                .FirstOrDefault(attr => attr.AbstractType.Equals(contractType))?.ImplementationType;

            if (!typeof(DefaultStoreAccessor).IsAssignableFrom(proxyType))
            {
                proxyType = null;
            }
            return new ContractFactory<TMessage>(proxyType);
        }
    }


    public interface IParameters
    {
        int MyProperty1 { get; set; }
        string MyProperty2 { get; set; }
        List<string> MyProperty6 { get; set; }
    }
    public class Parameters
    {
        public int MyProperty1 { get; set; }
        public string MyProperty2 { get; set; }
        public List<string> MyProperty6 { get; set; }
    }
    public class Component1 : ApplicationServiceComponent<IParameters>
    {
        public override Task<ServiceResponse> InvokeAsync(IApplicationServiceContext<IParameters> componentContext)
        {
            throw new NotImplementedException();
        }
    }
    public class Component2 : ApplicationServiceComponent<Parameters>
    {
        public override Task<ServiceResponse> InvokeAsync(IApplicationServiceContext<Parameters> componentContext)
        {
            throw new NotImplementedException();
        }
    }

}


