using System;
using System.Buffers.Text;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;
using K.Extensions;

namespace K.Serialization
{
    /// <summary>
    ///  Converts an value of DateTime to or from JSON according to the "R" standard format(ISO8601).<para/>
    ///  Additionally, This converter supports read <see cref="DateTime"/> from JavaScript Date object(based on Unix Time Stamp).<para/>
    ///  <see href="https://source.dot.net/#System.Text.Json/System/Text/Json/Serialization/Converters/Value/DateTimeConverter.cs"/>
    /// </summary>
    /// <remarks>
    ///     转换失败时返回默认值, 和不传递该参数的行为一致<para/>
    ///     可空值类型默认使用其值类型的 <see cref="JsonConverter{T}"/> <para/>
    ///     <see href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Text.Json/src/System/Text/Json/Serialization/Converters/Value/NullableConverter.cs"/>
    /// </remarks>
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        // Roundtrippable format. One of
        //
        //   012345678901234567890123456789012
        //   ---------------------------------
        //   2017-06-12T05:30:45.7680000-07:00      (DateTimeKind.Local)
        //   2017-06-12T05:30:45.7680000Z           (DateTimeKind.Utc)
        //   2017-06-12T05:30:45.7680000            (DateTimeKind.Unspecified)
        // Utf8Parser.TryParse(reader.ValueSpan, out DateTime value, out _, s_dateTimeStandardFormat.Symbol)
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

            // Unix Time Stamp: 表示自 1970 年 1 月 1 日 00:00:00 UTC(the Unix epoch)以来的毫秒数
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var unixTimeStamp)) 
            {
                return DateTime.UnixEpoch.AddMilliseconds(unixTimeStamp);
            }

            // ISO 8601-1:2019
            if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTime(out DateTime value))
            {
                return DateTimeUtil.ConvertTimeToUtc(value);
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
            var utcTime = DateTimeUtil.ConvertTimeToUtc(value);

            writer.WriteStringValue(utcTime);
        }

        public override DateTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = base.ReadAsPropertyName(ref reader, typeToConvert, options);

            return DateTimeUtil.ConvertTimeToUtc(value);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utcTime = DateTimeUtil.ConvertTimeToUtc(value);

            base.WriteAsPropertyName(writer, utcTime, options);
        }
    }
}
