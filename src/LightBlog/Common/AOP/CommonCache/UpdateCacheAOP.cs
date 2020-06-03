using Castle.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

            var method = invocation.Method;

            var cachingSetAttributes = method.GetCustomAttributes(true).Where(x => x.GetType() == typeof(CachingSetAttribute));
            foreach (CachingSetAttribute cachingSetAttribute in cachingSetAttributes) 
            {
                var parameterValues = new StringBuilder();
                ParameterInfo[] parameterInfos = method.GetParameters();

                for (int i = 0; i < cachingSetAttribute.QueryKeys.Length; i++)
                {
                    for (int ii = 0; ii < parameterInfos.Length; ii++)
                    {
                        if (cachingSetAttribute.QueryKeys[i].Equals(parameterInfos[ii].Name, StringComparison.OrdinalIgnoreCase))
                        {
                            parameterValues.Append(JsonConvert.SerializeObject(invocation.Arguments[ii]) + ":");
                        }
                    }
                }

                var key = $@"[{cachingSetAttribute.QuryTypeName}][{cachingSetAttribute.QueryMethodName}]:{parameterValues}";
                _cache.Remove(key);
            }
        }
    }
}
