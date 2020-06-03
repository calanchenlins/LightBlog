using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LightBlog.Common.AOP.CommonCache
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple =true, Inherited = true)]
    public class CachingSetAttribute : Attribute
    {
        public string QuryTypeName { get; set; }

        public string QueryMethodName { get; set; }

        public string[] QueryKeys { get; set; }
    }
}
