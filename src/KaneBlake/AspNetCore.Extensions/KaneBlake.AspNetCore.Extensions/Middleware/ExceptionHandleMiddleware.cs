using CoreWeb.Util.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.Middleware
{
    public class ExceptionHandlerCustomMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ApiBehaviorOptions _options;
        private readonly ILogger _logger;


        private static readonly MediaTypeHeaderValue _textHtmlMediaType = new MediaTypeHeaderValue("text/html");
        private static readonly MediaTypeHeaderValue _applicationProblemJsonMediaType = new MediaTypeHeaderValue("application/problem+json");

        public ExceptionHandlerCustomMiddleware(RequestDelegate next, ILogger<ExceptionHandlerCustomMiddleware> logger, IOptions<ApiBehaviorOptions> options)
        {
            _next = next;
            _options = options.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                //await Task.CompletedTask;
                throw;
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

            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            _logger.LogError(ex, "An error occurred while processing your request in path:{requestPath}, traceId:{traceId}", exceptionHandlerPathFeature.Path, traceId);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            if (!httpContext.Request.GetTypedHeaders().Accept.Any(a => a.IsSubsetOf(_textHtmlMediaType)))
            {
                httpContext.Response.ContentType = _textHtmlMediaType.MediaType.Value;

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
                httpContext.Response.ContentType = _applicationProblemJsonMediaType.MediaType.Value;

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError
                };


                if (_options.ClientErrorMapping.TryGetValue(StatusCodes.Status500InternalServerError, out var clientErrorData))
                {
                    problemDetails.Title ??= clientErrorData.Title;
                    problemDetails.Type ??= clientErrorData.Link;
                }


                if (traceId != null)
                {
                    problemDetails.Extensions["traceId"] = traceId;
                }

                var stream = httpContext.Response.Body;
                await System.Text.Json.JsonSerializer.SerializeAsync(stream, ServiceResponse.InnerException(ex));

            }
        }
    }
}
