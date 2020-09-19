﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KaneBlake.Basis.Common.Serialization
{
    /// <summary>
    ///  Converts an value of DateTime to or from JSON.
    ///  反序列化失败时返回默认值, 和不传递该参数的行为一致
    ///  <see href="https://docs.microsoft.com/zh-cn/dotnet/standard/datetime/system-text-json-support"/>
    /// </summary>
    public class DateTimeConverter : JsonConverter<DateTime>
    {
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

            if (reader.TokenType != JsonTokenType.String)
            {
                return default;
            }

            // ModelState validate failed when Exception occurs.
            if (!reader.TryGetDateTime(out DateTime value))
            {
                if (!DateTime.TryParse(reader.GetString(), out value))
                {
                    return default;
                }
            }
            return value;
        }

        /// <summary>
        /// Writes a value of DateTime as JSON.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
