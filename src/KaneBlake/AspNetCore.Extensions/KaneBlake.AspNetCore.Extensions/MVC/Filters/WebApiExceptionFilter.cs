using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.MVC.Filters
{
    public class WebApiExceptionFilter : IAsyncExceptionFilter, IOrderedFilter
    {
        private ILogger _logger;
        public int Order { get; set; } = int.MaxValue - 10;
        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.ExceptionHandled == false && context.Exception != null && context.ActionDescriptor.FilterDescriptors.Any(fd => fd.Filter is ApiControllerAttribute _))
            {
                if (_logger == null)
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    _logger = loggerFactory.CreateLogger<WebApiExceptionFilter>();
                }
                var ProblemDetailsFactory = context.HttpContext?.RequestServices?.GetRequiredService<ProblemDetailsFactory>();

                var problemDetails = ProblemDetailsFactory.CreateProblemDetails(
                    context.HttpContext,
                    statusCode: StatusCodes.Status500InternalServerError);
                context.Result = new ObjectResult(problemDetails)
                {
                    StatusCode = problemDetails.Status
                };
                context.ExceptionHandled = true;
                _logger.LogError(context.Exception, "An error occurred while processing your request in path:{0}", context.HttpContext.Request.Path.Value);
            }
            return Task.CompletedTask;
        }
    }
}
