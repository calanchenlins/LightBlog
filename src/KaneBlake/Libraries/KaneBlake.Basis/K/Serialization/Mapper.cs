using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace K.Serialization
{
    public class Mapper
    {
        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new JsonSerializerOptions() { ReferenceHandler = ReferenceHandler.IgnoreCycles };


        /// <summary>
        /// Execute a mapping from the source object to a new destination object based on <see cref="System.Text.Json.JsonSerializer"/>. 
        /// The source type is inferred from the source object.
        /// </summary>
        /// <typeparam name="TDestination">Destination type to create</typeparam>
        /// <param name="source">Source object to map from</param>
        /// <returns>Mapped destination object</returns>
        public static TDestination Map<TDestination>(object source)
        {
            var jsonString = JsonSerializer.Serialize(source, s_jsonSerializerOptions);

            return JsonSerializer.Deserialize<TDestination>(jsonString, s_jsonSerializerOptions);
        }
    }
}



#if PackageReference_Newtonsoft_Json
namespace K.Serialization 
{
    public class Mapper 
    {
        public static TDestination Map<TDestination>(object source)
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

        public class IgnoreComplexTypePropertiesResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var allProps = base.CreateProperties(type, memberSerialization);
                return allProps.Where(p => TypeDescriptor.GetConverter(p.PropertyType).CanConvertFrom(typeof(string))).ToList();
            }
        }
    }
}
#endif
