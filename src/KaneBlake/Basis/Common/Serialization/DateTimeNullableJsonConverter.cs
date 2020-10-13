using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaneBlake.Basis.Common.Serialization
{
    /// <summary>
    /// Converts an value of DateTime? to or from JSON.
    /// 可空值类型默认使用其值类型的 JsonConverter
    /// </summary>
    public class DateTimeNullableJsonConverter : JsonConverter<DateTime?>
    {
        /// <summary>
        /// Reads and converts the JSON to type DateTime?.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime?));

            if (reader.TokenType != JsonTokenType.String)
            {
                return default;
            }

            if (!reader.TryGetDateTime(out DateTime value))
            {
                var text = reader.GetString();
                if (!DateTime.TryParse(text, out value))
                {
                    if (!double.TryParse(text, out var unixTimeStamp))
                    {
                        return default;
                    }
                    value = DateTime.UnixEpoch.AddMilliseconds(unixTimeStamp);
                }

            }

            if (value.Kind.Equals(DateTimeKind.Unspecified))
            {
                value = value.ToLocalTime();
            }

            return value;
        }

        /// <summary>
        /// Writes a value of DateTime? as JSON.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value);
            }
        }
    }
}
