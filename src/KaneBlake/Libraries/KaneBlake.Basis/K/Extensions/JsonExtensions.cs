using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using K.Serialization;

namespace K.Extensions
{
    /// <summary>
    /// Extension Methods based on <see cref="System.Text.Json"/>
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Configure <see cref="System.Text.Json.JsonSerializerOptions"/> With default Vaule
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static JsonSerializerOptions Configure(this JsonSerializerOptions options)
        {
            // Use 'camelCase' casing.
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; 
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

            // In AspNetCore, when PropertyNamingPolicy's value is CamelCase or null, PropertyNameCaseInsensitive's value will be set true
            options.PropertyNameCaseInsensitive = true;

            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.Converters.Add(new StringTrimJsonConverter());
            // 2020-09-19T10:46:27.000+00:00  反序列化 => 本地时间    序列化   =>   本地时间(系统时区)   2020-09-19T18:46:27+08:00
            // 2020-09-19T10:46:27.000Z       反序列化 => UTC时间    |序列化  |=>  |UTC时间             2020-09-19T10:46:27Z
            // 2020-09-19T10:46:27.000        反序列化 => 时区未确定  序列化   =>   时区未确定           2020-09-19T10:46:27.000
            options.Converters.Add(new DateTimeJsonConverter());
            options.Converters.Add(new DateTimeOffsetJsonConverter());

            if (options.Encoder is null)
            {
                options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            }

            return options;
        }
    }
}
