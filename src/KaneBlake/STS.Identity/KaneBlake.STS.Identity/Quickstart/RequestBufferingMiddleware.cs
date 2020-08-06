using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KaneBlake.Basis.Extensions.Common;

namespace KaneBlake.STS.Identity.Quickstart
{
    public class RequestBufferingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestBufferingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestBufferingMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            try
            {
                // https://github.com/dotnet/aspnetcore/issues/24562
                // Before reads to the end of HttpContext.Request.BodyReader(When ReadResult.IsCompleted is true)
                // We shouldn't sets the position within HttpContext.Request.Body
                // Otherwise, HttpContext.Request.BodyReader will repeatedly reads the stream after the Position 
                var buffer = await context.Request.BodyReader.ReadToEndAsync();

                // log Request.Body's content...
            }
            catch (Exception ex) 
            {
                _logger.LogWarning(ex, "failed to log Request.Body.");
            }
            finally 
            {
                // we have to sets the position of HttpContext.Request.Body before reads HttpContext.Request.Body, 
                context.Request.Body.Position = 0;
            }

            // Call the next delegate/middleware in the pipeline
            await _next(context);

        }
    }
}
