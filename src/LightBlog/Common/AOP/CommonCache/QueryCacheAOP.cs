using Castle.DynamicProxy;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
            var method = invocation.Method;

            if (method.GetCustomAttributes(true).FirstOrDefault(x => x.GetType() == typeof(CachingAttribute)) 
                is CachingAttribute cachingAttribute)
            {
                var parameterValues = new StringBuilder();
                ParameterInfo[] parameterInfos = method.GetParameters();

                for (int i = 0; i < cachingAttribute.QueryKeys.Length; i++) 
                {
                    for (int ii = 0; ii < parameterInfos.Length; ii++)
                    {
                        if (cachingAttribute.QueryKeys[i].Equals(parameterInfos[ii].Name, StringComparison.OrdinalIgnoreCase))
                        {
                            parameterValues.Append(JsonConvert.SerializeObject(invocation.Arguments[ii]) +":");
                        }
                    }
                }

                var key = $@"[{invocation.TargetType.Name}][{method.Name}]:{parameterValues}";

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
