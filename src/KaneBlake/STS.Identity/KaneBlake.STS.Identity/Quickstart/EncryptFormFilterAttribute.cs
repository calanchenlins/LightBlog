using IdentityServer4.Stores;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KaneBlake.Basis.Extensions.Cryptography;
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

namespace KaneBlake.STS.Identity.Quickstart
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EncryptFormFilterAttribute : Attribute,IAsyncResourceFilter
    {
        private readonly EncryptFormValueProviderFactory _valueProviderFactory;

        public EncryptFormFilterAttribute(EncryptFormValueProviderFactory valueProviderFactory)
        {
            _valueProviderFactory = valueProviderFactory ?? throw new ArgumentNullException(nameof(valueProviderFactory));

        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            //var _ = context.HttpContext.Request.Headers["Content-Type"].Any(v => "application/x-msgpack".Equals(v));
            var formEncrypted = context.HttpContext.Request.Headers["form-data-format"].Any(v => "EncryptionForm".Equals(v));
            if (formEncrypted)
            {
                await DecryptBodyAsync(context.HttpContext);
            }
            //var formValueProviderFactory = context.ValueProviderFactories
            //    .OfType<FormValueProviderFactory>()
            //    .FirstOrDefault();
            //if (formValueProviderFactory != null)
            //{
            //    context.ValueProviderFactories.Remove(formValueProviderFactory);
            //}

            //var encryptFormValueProviderFactory = context.ValueProviderFactories
            //    .OfType<EncryptFormValueProviderFactory>()
            //    .FirstOrDefault();
            //if (encryptFormValueProviderFactory == null)//&& context.HttpContext.Request.HasFormContentType
            //{
            //    // 默认按照顺序查找value
            //    context.ValueProviderFactories.Insert(0, _valueProviderFactory);
            //}

            var resultContext = await next();
            // Do something after the action executes.
        }

        private async Task DecryptBodyAsync(HttpContext httpContext)
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
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
