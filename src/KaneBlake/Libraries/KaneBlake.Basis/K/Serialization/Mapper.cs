using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
