using K.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace K.Extensions
{
    public static class ObjectUtil
    {
        /// <summary>
        /// 设置对象默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T SetDefaultValue<T>(T obj)
        {
            var props = typeof(T).GetProperties();
            foreach (var prop in props)
            {
                // 索引器属性
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                var propType = prop.PropertyType;

                // 值类型
                if (propType.IsValueType)
                {
                    // 可空值类型
                    if (prop.GetValue(obj) == null)
                    {
                        var underlyingType = Nullable.GetUnderlyingType(propType);
                        if (underlyingType != null)
                        {
                            propType = underlyingType;
                            prop.SetValue(obj, Activator.CreateInstance(underlyingType));
                        }
                    }


                    if (propType == typeof(DateTime) && (DateTime)prop.GetValue(obj) < SqlDateTime.MinValue.Value)
                    {
                        prop.SetValue(obj, SqlDateTime.MinValue.Value);
                    }
                }
                // 引用类型
                else if (prop.GetValue(obj) == null)
                {
                    if (propType == typeof(string))
                    {
                        prop.SetValue(obj, string.Empty);
                    }
                }
            }

            return obj;
        }


        public static IDictionary<string, object> GetDictionary(object source)
        {
            var dict = new Dictionary<string, object>();
            var props = source.GetType().GetProperties();
            foreach (var prop in props)
            {
                // 索引器属性
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (dict.ContainsKey(prop.Name))
                {
                    continue;
                }

                var propType = prop.PropertyType;
                //if (!TypeDescriptor.GetConverter(propType).CanConvertFrom(typeof(string)))
                //{
                //    continue;
                //}

                var value = prop.GetValue(source);

                if (value == null)
                {
                    // 可空值类型
                    if (propType.IsValueType)
                    {
                        var underlyingType = Nullable.GetUnderlyingType(propType);

                        Debug.Assert(underlyingType != null);

                        value = Activator.CreateInstance(underlyingType);
                    }
                    else if (propType.Equals(typeof(string)))
                    {
                        value = string.Empty;
                    }
                }

                dict.Add(prop.Name, value);
            }

            return dict;
        }
    }
}
