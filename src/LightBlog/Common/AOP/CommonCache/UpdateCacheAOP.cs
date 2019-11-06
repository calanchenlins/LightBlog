using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Common.AOP.CommonCache
{
    public class UpdateCacheAOP : IInterceptor
    {
        private readonly IMemoryCache _cache;

        public UpdateCacheAOP(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();

            if (invocation.Arguments.Length == 0)
            {
                return;
            }

            var method = invocation.MethodInvocationTarget ?? invocation.Method;
            
            if (method.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(CachingSetAttribute)) is CachingSetAttribute authAttribute)
            {
                var sourceId = JsonConvert.SerializeObject(invocation.Arguments[0]);

                var key = $@"[{authAttribute.QuryType.Name}][{authAttribute.QueryMethodName}]:{sourceId}";

                _cache.Remove(key);
            }
        }
    }
}
