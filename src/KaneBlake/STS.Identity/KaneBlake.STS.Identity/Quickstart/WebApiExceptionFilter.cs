using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Quickstart
{
    public class WebApiExceptionFilter : IActionFilter, IOrderedFilter
    {
        private ILogger _logger;
        public WebApiExceptionFilter()
        {
        }

        public int Order { get; set; } = int.MaxValue - 10;

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
            if (context.Exception != null && context.ActionDescriptor.FilterDescriptors.Any(fd => fd.Filter is ApiControllerAttribute _))
            {
                if (_logger == null) 
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    _logger = loggerFactory.CreateLogger<WebApiExceptionFilter>();
                }
                var ProblemDetailsFactory = context.HttpContext?.RequestServices?.GetRequiredService<ProblemDetailsFactory>();
                
                var problemDetails = ProblemDetailsFactory.CreateProblemDetails(
                    context.HttpContext,
                    statusCode: (int)HttpStatusCode.InternalServerError,
                    detail:context.Exception.Message);
                context.Result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status
                };
                context.ExceptionHandled = true;
                _logger.LogError(context.Exception, "An error occurred while processing your request.");
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }
    }
}
