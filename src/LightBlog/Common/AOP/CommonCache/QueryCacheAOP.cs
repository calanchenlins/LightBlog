using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Common.AOP.CommonCache
{
    public class QueryCacheAOP :IInterceptor
    {
        private readonly IMemoryCache _cache;

        public QueryCacheAOP(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public void Intercept(IInvocation invocation)
        {
            var a = invocation.GetConcreteMethod();
            var b = invocation.GetConcreteMethodInvocationTarget();
            var c = invocation.Method;
            var d = invocation.MethodInvocationTarget;

            var e = invocation.TargetType;
            var f = invocation.GetType();

            var method = invocation.MethodInvocationTarget ?? invocation.Method;

            if (method.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(CachingAttribute)) 
                is CachingAttribute authAttribute && invocation.Arguments.Length > 0)
            {
                var sourceId = invocation.Arguments[0];

                var sourceIdStr = JsonConvert.SerializeObject(sourceId);

                var key = $@"[{invocation.TargetType.Name}][{invocation.MethodInvocationTarget.Name}]:{sourceIdStr}";

                if (_cache.TryGetValue(key, out object cacheValue))
                {
                    invocation.ReturnValue = cacheValue;
                }
                else
                {
                    invocation.Proceed();
                    _cache.Set(key, invocation.ReturnValue,new TimeSpan(TimeSpan.TicksPerDay));
                }
            }
            else
            {
                invocation.Proceed();
            }
        }
    }
}
