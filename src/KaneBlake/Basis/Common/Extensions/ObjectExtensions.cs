using KaneBlake.Basis.Common.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace KaneBlake.Basis.Common.Extensions
{
    public static class ObjectExtensions
    {
        public static TDestination Map<TDestination>(this object source)
        {
            var JsonSerializerSettings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            JsonSerializerSettings.ContractResolver = new IgnoreComplexTypePropertiesResolver();

            var JText = JsonConvert.SerializeObject(source, JsonSerializerSettings);
            var deserializeSettings = new JsonSerializerSettings
            {
                Error = (sender, errorEventArgs) => { errorEventArgs.ErrorContext.Handled = true; }
            };
            var JObject = JsonConvert.DeserializeObject<TDestination>(JText, deserializeSettings);

            return JObject;
        }


        public static void SetDefaultValue(this object source)
        {
            var modelType = source.GetType();
            var props = modelType.GetProperties();
            foreach (var prop in props)
            {
                var propType = prop.PropertyType;

                // [Required]引用类型 赋初始值
                if (prop.GetCustomAttribute<RequiredAttribute>() is RequiredAttribute attr
                    && prop.GetValue(source) is null)
                {
                    if (propType == typeof(string))
                    {
                        prop.SetValue(source, string.Empty);
                    }
                }
            }
        }


        public static IDictionary<string, object> GetVars(this object source)
        {
            var dict = new Dictionary<string, object>();
            var props = source.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (!dict.ContainsKey(prop.Name))
                {
                    var propType = prop.PropertyType;
                    if (!TypeDescriptor.GetConverter(propType).CanConvertFrom(typeof(string)))
                    {
                        continue;
                    }

                    var value = prop.GetValue(source);
                    if (value == null)
                    {
                        if (propType.Equals(typeof(string)))
                        {
                            value = string.Empty;
                        }
                        if (propType.IsValueType)
                        {
                            value = Activator.CreateInstance(prop.PropertyType);
                            if (value == null && prop.PropertyType.GenericTypeArguments.Length > 0 && prop.PropertyType.GenericTypeArguments[0].IsValueType)
                            {
                                value = Activator.CreateInstance(prop.PropertyType.GenericTypeArguments[0]);
                            }
                        }
                    }

                    dict.Add(prop.Name, value);
                }
            }
            return dict;
        }
    }
}
