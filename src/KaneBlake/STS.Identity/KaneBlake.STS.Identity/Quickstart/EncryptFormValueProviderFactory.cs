using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KaneBlake.Basis.Extensions.Cryptography;
using KaneBlake.STS.Identity.Common;
using MessagePack;
using static MessagePack.MessagePackSerializer;
using System.Text;

namespace KaneBlake.STS.Identity.Quickstart
{
    public class EncryptFormValueProviderFactory : IValueProviderFactory
    {

        public EncryptFormValueProviderFactory()
        {
        }

        /// <inheritdoc />
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.ActionContext.HttpContext.Request;
            if (request.ContentType.Equals("application/x-msgpack") 
                && request.Headers.Any(h => "form-data-format".Equals(h.Key) && h.Value.Any(v => "EncryptionForm".Equals(v))) )
            {
                // Allocating a Task only when the body is form data with RsaEncryption.
                return AddValueProviderAsync(context);
            }

            return Task.CompletedTask;
        }

        private static async Task AddValueProviderAsync(ValueProviderFactoryContext context)
        {
            var request = context.ActionContext.HttpContext.Request;
            IFormCollection form;
            try
            {
                request.Body.Position = 0;
                var body = await request.BodyReader.ReadAsync();
                var CiphertextArray = MessagePackSerializer.Deserialize<byte[][]>(body.Buffer, MessagePack.Resolvers.ContractlessStandardResolver.Options);
                var plainText = new StringBuilder();

                for (int i = 0; i < CiphertextArray.Length; i++)
                {
                    plainText.Append(AppInfo.Instance.Certificate.DecryptFromUTF8bytes(CiphertextArray[i]));
                }

                var formReader = new FormReader(plainText.ToString());
                var formFields = await formReader.ReadFormAsync();
                form = new FormCollection(formFields);
            }
            catch (Exception)
            {
                throw;
                //throw new ValueProviderException(Resources.FormatFailedToReadRequestForm(ex.Message), ex);
            }

            var valueProvider = new FormValueProvider(
                BindingSource.Form,
                form,
                CultureInfo.CurrentCulture);

            context.ValueProviders.Add(valueProvider);
        }
    }
}
