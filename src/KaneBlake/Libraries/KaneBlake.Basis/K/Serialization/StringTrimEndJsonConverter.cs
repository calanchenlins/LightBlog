using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace K.Serialization
{
    /// <summary>
    /// Converts an value of string to or from JSON.
    /// </summary>
    public class StringTrimEndJsonConverter : JsonConverter<string>
    {
        /// <summary>
        /// Reads and converts the JSON to type string.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString()?.TrimEnd();
        }

        /// <summary>
        /// Writes a value of string as JSON.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.TrimEnd());
        }
    }
}
