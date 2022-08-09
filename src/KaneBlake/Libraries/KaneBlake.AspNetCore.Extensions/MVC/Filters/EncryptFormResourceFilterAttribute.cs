using MessagePack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using K.Basis.Common.Cryptography;

namespace K.AspNetCore.Extensions.MVC.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EncryptFormResourceFilterAttribute : Attribute, IAsyncResourceFilter
    {
        private readonly ILogger _logger;

        private readonly X509Certificate2 _certificate;

        public EncryptFormResourceFilterAttribute(ILogger<EncryptFormResourceFilterAttribute> logger, X509Certificate2 certificate)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
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

                var blockLength = _certificate.GetRSAPrivateKey().ExportParameters(false).Modulus.Length;
                List<byte> raw = new List<byte>(blockLength / 2);
                for (int i = 0; i < CiphertextArray.Length; i++)
                {
                    raw.AddRange(_certificate.Decrypt(CiphertextArray[i]).ToArray());
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
}
