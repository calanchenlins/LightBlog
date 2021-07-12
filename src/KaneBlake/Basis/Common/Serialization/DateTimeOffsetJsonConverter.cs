using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaneBlake.Basis.Common.Serialization
{
    /// <summary>
    /// Converts an value of DateTimeOffset to or from JSON.
    /// </summary>
    public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
    {
        /// <summary>
        /// Reads and converts the JSON to type DateTimeOffset.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTimeOffset));

            if (reader.TokenType != JsonTokenType.String || reader.TokenType != JsonTokenType.Number)
            {
                return default;
            }

            if (!reader.TryGetDateTimeOffset(out DateTimeOffset value))
            {
                var text = reader.GetString();
                if (!DateTimeOffset.TryParse(text, out value))
                {
                    if (!double.TryParse(text, out var unixTimeStamp))
                    {
                        return default;
                    }
                    value = DateTimeOffset.UnixEpoch.AddMilliseconds(unixTimeStamp);
                }

            }

            return value;
        }

        /// <summary>
        /// Writes a value of DateTimeOffset as JSON.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
