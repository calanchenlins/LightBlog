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

        public string QuryTypeName { get; set; }

        public string[] QueryMethodsName { get; set; }

        public string[] QueryKeys { get; set; }
    }
}
