using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace KaneBlake.Basis.Common.Serialization
{
    public class IgnoreComplexTypePropertiesResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var allProps = base.CreateProperties(type, memberSerialization);
            return allProps.Where(p => TypeDescriptor.GetConverter(p.PropertyType).CanConvertFrom(typeof(string))).ToList();
        }
    }
}
