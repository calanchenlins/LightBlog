using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.Services.Module
{
    internal class ApplicationServiceInnerContext : IApplicationServiceContext
    {
        public ApplicationServiceInnerContext(IDictionary<string, object> sharedStore)
        {
            SharedStore = sharedStore ?? throw new ArgumentNullException(nameof(sharedStore));
        }

        /// <summary>
        /// 用户输入、组件间状态共享
        /// </summary>
        public IDictionary<string, object> SharedStore { get; }

        /// <summary>
        /// 服务响应
        /// </summary>
        public IDictionary<string, object> ResponseStore { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();
    }

    internal class ApplicationServiceContext : IApplicationServiceContext
    {
        public IDictionary<object, object> Items { get; set; }
    }

    internal class ApplicationServiceContext<TParameter> : ApplicationServiceContext, IApplicationServiceContext<TParameter>
    {
        public TParameter Parameter { get; set; }
    }


    internal class ApplicationServiceContextWithReturnValue<TReturnValue> : ApplicationServiceContext, IApplicationServiceContextWithReturnValue<TReturnValue>
    {
        public TReturnValue ReturnValue { get; set; }
    }


    internal class ApplicationServiceContext<TParameter, TReturnValue> : ApplicationServiceContext<TParameter>, IApplicationServiceContext<TParameter, TReturnValue>
    {
        public TReturnValue ReturnValue { get; set; }
    }

}
