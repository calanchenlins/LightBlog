using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace KaneBlake.Basis.Extensions.Hangfire
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class JobTemplateAttribute : Attribute
    {
        public JobTemplateAttribute():this(null)
        {
        }
        public JobTemplateAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType { get; }
    }
}
