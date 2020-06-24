using KaneBlake.STS.Identity.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Quickstart
{
    public class ExceptionCustomHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiBehaviorOptions _options;
        private readonly ILogger _logger;

        public ExceptionCustomHandlerMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<ApiBehaviorOptions> options)
        {
            _next = next;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ExceptionCustomHandlerMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // await _next(context);//Status404NotFound Middleware
                await HandleException(context);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An error occurred while handle exception.");
                await Task.CompletedTask;
            }

        }


        private async Task HandleException(HttpContext httpContext)
        {
            var exceptionHandlerPathFeature = httpContext.Features.Get<IExceptionHandlerPathFeature>();
            var ex = exceptionHandlerPathFeature?.Error;
            if (ex == null)
            {
                await Task.CompletedTask;
            }

            _logger.LogError(ex, "An error occurred while processing your request in path:{0}", exceptionHandlerPathFeature.Path);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            if (!httpContext.Request.GetTypedHeaders().Accept.Any(a => a.IsSubsetOf(AppInfo.TextHtmlMediaType)))
            {
                httpContext.Response.ContentType = AppInfo.TextHtmlMediaType.MediaType.Value;

                await httpContext.Response.WriteAsync("<html lang=\"en\"><body>\r\n");
                await httpContext.Response.WriteAsync("ERROR!<br><br>\r\n");

                if (exceptionHandlerPathFeature?.Error is FileNotFoundException)
                {
                    await httpContext.Response.WriteAsync("File error thrown!<br><br>\r\n");
                }

                await httpContext.Response.WriteAsync("<a href=\"/\">Home</a><br>\r\n");
                await httpContext.Response.WriteAsync("</body></html>\r\n");

            }
            else
            {
                httpContext.Response.ContentType = AppInfo.ApplicationProblemJsonMediaType.MediaType.Value;

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError
                };


                if (_options.ClientErrorMapping.TryGetValue(StatusCodes.Status500InternalServerError, out var clientErrorData))
                {
                    problemDetails.Title ??= clientErrorData.Title;
                    problemDetails.Type ??= clientErrorData.Link;
                }

                var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
                if (traceId != null)
                {
                    problemDetails.Extensions["traceId"] = traceId;
                }

                var stream = httpContext.Response.Body;
                await System.Text.Json.JsonSerializer.SerializeAsync(stream, problemDetails);

            }
        }
    }
}
