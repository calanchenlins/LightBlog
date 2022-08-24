using System;
using System.Buffers.Text;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace K.Serialization
{
    /// <summary>
    ///  Converts an value of DateTime to or from JSON according to the "R" standard format(ISO8601).<para/>
    ///  Additionally, This converter supports read <see cref="DateTime"/> from JavaScript Date object(based on Unix Time Stamp).<para/>
    /// </summary>
    /// <remarks>
    ///     转换失败时返回默认值, 和不传递该参数的行为一致<para/>
    ///     可空值类型默认使用其值类型的 <see cref="JsonConverter{T}"/> <para/>
    ///     <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/NullableConverter.cs"/>
    /// </remarks>
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        // DateTimeKind.Utc:         2017-06-12T05:30:45.7680000Z
        // DateTimeKind.Local:       2017-06-12T05:30:45.7680000+08:00
        // DateTimeKind.Unspecified: 2017-06-12T05:30:45.7680000
        private static readonly StandardFormat s_dateTimeStandardFormat = new StandardFormat('O');

        /// <summary>
        /// Reads and converts the JSON to type DateTime.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime));


            // JavaScript Time Stamp
            if (reader.TokenType == JsonTokenType.Number) 
            {
                var javaScriptTimeStamp = reader.GetInt64();

                return DateTimeOffset.FromUnixTimeMilliseconds(javaScriptTimeStamp).UtcDateTime;
            }
            // ISO8601 UTC
            else if (reader.TokenType == JsonTokenType.String)
            {
                if (Utf8Parser.TryParse(reader.ValueSpan, out DateTime value, out _, s_dateTimeStandardFormat.Symbol))
                {
                    return GetUniversalTime(value);
                }
            }


            // ModelState validate failed when Exception occurs.
            throw new JsonException($"The JSON value is not in a supported {nameof(DateTime)} format.");
        }

        /// <summary>
        /// Writes a value of DateTime as JSON.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utcTime = GetUniversalTime(value);
            Span<byte> tempSpan = stackalloc byte[28];

            bool result = Utf8Formatter.TryFormat(utcTime, tempSpan, out _, s_dateTimeStandardFormat);
            Debug.Assert(result);

            writer.WriteStringValue(tempSpan);
        }


        private static DateTime GetUniversalTime(DateTime value) 
        {
            if (value.Kind.Equals(DateTimeKind.Utc))
            {
                return value;
            }
            else if (value.Kind.Equals(DateTimeKind.Local))
            {
                return value.ToUniversalTime();
            }
            else
            {
                return new DateTime(value.Ticks, DateTimeKind.Utc);
            }
        }
    }
}
