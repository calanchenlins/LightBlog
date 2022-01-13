using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KaneBlake.Basis.Common.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// 获取接口属性
        /// </summary>
        /// <param name="type"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        public static IEnumerable<PropertyInfo> GetInterfaceProperties(this Type type, BindingFlags bindingFlags)
        {
            if (!type.IsInterface)
            {
                yield break;
            }

            var typeInfo = type.GetTypeInfo();
            IEnumerable<PropertyInfo> sourceProperties = typeInfo.GetProperties(bindingFlags)
                .Concat(typeInfo.ImplementedInterfaces.SelectMany(x => x.GetTypeInfo().GetProperties(bindingFlags)));

            foreach (var prop in sourceProperties)
                yield return prop;
        }
    }
}
