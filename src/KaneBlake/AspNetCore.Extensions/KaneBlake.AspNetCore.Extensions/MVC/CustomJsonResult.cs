using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.MVC
{
    //public class CustomJsonResult : JsonResult
    //{
    //    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    //    {
    //        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    //        Converters = new List<JsonConverter> {
    //            new StringEnumConverter(),
    //            new IsoDateTimeConverter()
    //            {
    //                DateTimeFormat="yyyy-MM-dd HH:mm:ss"
    //            },
    //            new TrimConverter()
    //        },
    //        //DateFormatString = "yyyy-MM-dd HH:mm:ss",
    //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    //        Formatting = Formatting.None
    //    };

    //    public CustomJsonResult(object value)
    //        :base(value)
    //    {
    //    }

    //    public override void ExecuteResult(ActionContext context)
    //    {
    //        if (context == null)
    //        {
    //            throw new ArgumentNullException("context");
    //        }

    //        var response = context.HttpContext.Response;

    //        if (!string.IsNullOrEmpty(ContentType))
    //        {
    //            response.ContentType = ContentType;
    //        }
    //        else
    //        {
    //            response.ContentType = "application/json";
    //        }
    //        if (ContentEncoding != null)
    //        {
    //            response.ContentEncoding = ContentEncoding;
    //        }
    //        if (Value != null)
    //        {
    //            response.BodyWriter.WriteAsync(JsonConvert.SerializeObject(Value, Settings));
    //        }
    //    }


    //    public override Task ExecuteResultAsync(ActionContext context)
    //    {
    //        return base.ExecuteResultAsync(context);
    //    }

    //    private class TrimConverter : JsonConverter
    //    {
    //        public override bool CanConvert(Type objectType)
    //        {
    //            return typeof(string) == objectType;
    //        }

    //        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //        {
    //            if (reader.TokenType.Equals(JsonToken.String) && reader.Value != null)
    //            {
    //                return (reader.Value as string).Trim();
    //            }

    //            return reader.Value;
    //        }

    //        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //        {
    //            if (value is string text)
    //            {
    //                if (text == null)
    //                {
    //                    writer.WriteNull();
    //                }
    //                else
    //                {
    //                    writer.WriteValue(text.Trim());
    //                }

    //            }
    //        }
    //    }
    //}





}
