using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.MVC.ViewFeatures
{
    public class TypedJsonTempDataSerializer : TempDataSerializer
    {
        public override bool CanSerializeType(Type type) => true;

        public override IDictionary<string, object> Deserialize(byte[] unprotectedData)
        {
            var json = Encoding.UTF8.GetString(unprotectedData);
            var tempData = Newtonsoft.Json.JsonConvert.DeserializeObject<IDictionary<string, TempDataValueWrapper>>(json);
            var dict = new Dictionary<string, object>(tempData.Count);
            foreach (var kv in tempData)
            {
                if (typeof(JContainer).IsAssignableFrom(kv.Value.Value.GetType()))
                {
                    var valueType = Type.GetType(kv.Value.ValueType);
                    var jsonValue = kv.Value.Value.ToString();
                    var objectValue = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonValue, valueType);
                    dict.Add(kv.Key, objectValue);
                }
                else
                {
                    dict.Add(kv.Key, kv.Value.Value);
                }
            }
            return dict;
        }

        public override byte[] Serialize(IDictionary<string, object> values)
        {
            var dict = new Dictionary<string, object>(values.Count);
            foreach (var kv in values)
            {
                if (kv.Value != null)
                {
                    dict.Add(kv.Key, new TempDataValueWrapper(kv.Value));
                }
            }
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(dict);
            return Encoding.UTF8.GetBytes(json);
        }
    }
    public class TempDataValueWrapper
    {
        public TempDataValueWrapper()
        {
        }
        public TempDataValueWrapper(object value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ValueType = value.GetType().AssemblyQualifiedName;
        }

        public string ValueType { get; set; }
        public object Value { get; set; }

    }
}
