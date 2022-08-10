using System;
using System.Collections.Generic;
using System.Text;

namespace K.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class AutoImplementationAttribute : Attribute
    {

    }
}
