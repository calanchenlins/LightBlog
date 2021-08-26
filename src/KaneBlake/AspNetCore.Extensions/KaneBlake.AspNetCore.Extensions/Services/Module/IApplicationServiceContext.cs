using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.Services.Module
{
    /// <summary>
    /// Defines an interface that represents a application service context.
    /// </summary>
    public interface IApplicationServiceContext
    {
        /// <summary>
        /// Gets the collection of items used to communicate with other <see cref="IApplicationServiceComponent"/>s.
        /// </summary>
        public IDictionary<object, object> Items { get; }

        public void AddOrUpdateComponentCahce(IApplicationServiceComponent component, object value)
        {
            Items[component.Name] = value;
        }
    }


    /// <summary>
    /// Defines an interface that represents a application service context with parameter.
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter.</typeparam>
    public interface IApplicationServiceContext<out TParameter> : IApplicationServiceContext
    {
        /// <summary>
        /// Gets the <typeparamref name="TParameter"/> that is associated with this context.
        /// </summary>
        public TParameter Parameter { get; }
    }

    /// <summary>
    /// Defines an interface that represents a application service context with return value.
    /// </summary>
    /// <typeparam name="TReturnValue">The type of return value.</typeparam>
    public interface IApplicationServiceParameterlessContext<out TReturnValue> : IApplicationServiceContext
    {
        /// <summary>
        /// Gets the <typeparamref name="TReturnValue"/> that is associated with this context.
        /// </summary>
        public TReturnValue ReturnValue { get; }
    }

    /// <summary>
    /// Defines an interface that represents a application service context with parameter and return value.
    /// </summary>
    /// <typeparam name="TParameter">The type of parameter.</typeparam>
    /// <typeparam name="TReturnValue">The type of return value.</typeparam>
    public interface IApplicationServiceContext<out TParameter, out TReturnValue> : IApplicationServiceContext<TParameter>, IApplicationServiceParameterlessContext<TReturnValue>
    {
    }

}
