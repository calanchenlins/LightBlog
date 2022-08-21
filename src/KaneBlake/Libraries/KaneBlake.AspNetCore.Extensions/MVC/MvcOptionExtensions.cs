using K.AspNetCore.Extensions.MVC.Filters;
using K.AspNetCore.Extensions.MVC.ModelBinding.Binders;
using K.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.MVC
{
    public static class MvcOptionExtensions
    {
        public static MvcOptions Configure(this MvcOptions options) 
        {
            options.Filters.Add<InjectResultActionFilter>();
            options.Conventions.Add(new InvalidModelStateFilterConvention());
            options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
            options.ModelBinderProviders.Insert(0, new DateTimeOffsetModelBinderProvider());

            return options;
        }



        /// <summary>
        /// Customize InvalidModelState response with <seealso cref="ServiceProblemResponse{T}"/>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ApiBehaviorOptions Configure(this ApiBehaviorOptions options)
        {
            // avoid adding duplicate Convention: InvalidModelStateFilterConvention
            options.SuppressModelStateInvalidFilter = false;
            options.InvalidModelStateResponseFactory = context =>
            {
                var response = ServiceResponse.BadRequest(new SerializableModelError(context.ModelState));
                var traceId = Activity.Current?.Id ?? context.HttpContext?.TraceIdentifier;
                response.TryAddTraceId(traceId);
                var result = new ObjectResult(response);
                result.ContentTypes.Add("application/problem+json");
                result.ContentTypes.Add("application/problem+xml");
                return result;
            };

            return options;
        }
    }
}
