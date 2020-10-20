using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.Build.Core.Localization
{
    public class LocalizationMethod
    {
        public LocalizationMethod(string typeName, string methodName, bool isGenericType)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            IsGenericType = isGenericType;
        }

        public string TypeName { get; set; }

        public string MethodName { get; set; }

        public bool IsGenericType { get; set; }
    }
}
