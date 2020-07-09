using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

#if DEBUG

            try
            {
                var body = await context.Request.BodyReader.ReadAsync();
                var planText = Encoding.UTF8.GetString(body.Buffer.ToArray());
            }
            catch (Exception) { }
            finally 
            {
                context.Request.Body.Position = 0;
            }
            
#endif


            // Call the next delegate/middleware in the pipeline
            await _next(context);
        }
    }
}
