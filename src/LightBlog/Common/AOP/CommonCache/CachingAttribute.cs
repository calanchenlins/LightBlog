using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LightBlog.Common.AOP.CommonCache
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CachingAttribute : Attribute
    {
        public string[] QueryKeys { get; set; }
    }
}
