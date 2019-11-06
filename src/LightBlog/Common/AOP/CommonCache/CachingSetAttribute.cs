using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LightBlog.Common.AOP.CommonCache
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class CachingSetAttribute : Attribute
    {

        public Type QuryType { get; set; }

        public string QueryMethodName { get; set; }
    }
}
