using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KaneBlake.Basis.Common.Cryptography;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using MessagePack;
using System.Text;
using KaneBlake.STS.Identity.Common;
using System.IO;
using System.IO.Pipelines;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using KaneBlake.Basis.Services;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KaneBlake.STS.Identity.Quickstart
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EncryptFormResourceFilterAttribute : Attribute,IAsyncResourceFilter
    {
        private readonly ILogger _logger;

        public EncryptFormResourceFilterAttribute(ILogger<EncryptFormResourceFilterAttribute> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var formEncrypted = context.HttpContext.Request.Headers["form-data-format"].Any(v => "EncryptionForm".Equals(v));
            if (formEncrypted)
            {
                await DecryptBodyAsync(context.HttpContext);
            }

            var resultContext = await next();
            // Do something after the action executes.
        }

        private async Task<int> DecryptBodyAsync(HttpContext httpContext)
        {
            try
            {
                var request = httpContext.Request;
                request.EnableBuffering();
                request.Body.Position = 0;
                var body = await request.BodyReader.ReadAsync();
                var CiphertextArray = MessagePackSerializer.Deserialize<byte[][]>(body.Buffer, MessagePack.Resolvers.ContractlessStandardResolver.Options);
                var plainText = new StringBuilder();

                var blockLength = AppInfo.Instance.Certificate.GetRSAPrivateKey().ExportParameters(false).Modulus.Length;
                List<byte> raw = new List<byte>(blockLength / 2);
                for (int i = 0; i < CiphertextArray.Length; i++)
                {
                    raw.AddRange(AppInfo.Instance.Certificate.Decrypt(CiphertextArray[i]).ToArray());
                }
                var originalBody = request.Body;
                var memoryStream = new MemoryStream(raw.ToArray());
                request.Body = memoryStream;
                request.HttpContext.Response.RegisterForDispose(memoryStream);
                await originalBody.DisposeAsync();
                return 1;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

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
                && !serviceResponse.Extensions.ContainsKey("traceId"))
            {
                var traceId = Activity.Current?.Id ?? executedContext.HttpContext?.TraceIdentifier;
                if (!serviceResponse.Extensions.TryAdd("traceId", traceId))
                {
                    _logger.LogWarning("append traceId:'{traceId}' to ServiceResponse.Extensions failed. ServiceResponse:{serviceResponse}", traceId, serviceResponse);
                }

            }

        }
    }
}
