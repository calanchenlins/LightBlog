using KaneBlake.Basis.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.MVC.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class InjectResultActionFilter : Attribute, IAsyncActionFilter, IOrderedFilter
    {
        private readonly ILogger _logger;

        public InjectResultActionFilter(ILogger<InjectResultActionFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public int Order { get; set; } = 1;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            // Inject HtttpContext's Info to ServiceResponse
            if (!executedContext.Canceled && executedContext.Exception == null
                && executedContext.Result is ObjectResult response // exclude JsonResult
                && response.Value is ServiceResponse serviceResponse
                && !serviceResponse.OKStatus
                && !serviceResponse.ContainsTraceId())
            {
                var traceId = Activity.Current?.Id ?? executedContext.HttpContext?.TraceIdentifier;
                if (!serviceResponse.TryAddTraceId(traceId))
                {
                    _logger.LogWarning("append traceId:'{traceId}' to ServiceResponse.Extensions failed. ServiceResponse:{serviceResponse}", traceId, serviceResponse);
                }

            }

        }
    }
}
