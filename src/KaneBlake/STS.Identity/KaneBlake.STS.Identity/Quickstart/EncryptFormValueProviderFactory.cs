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
            if (request.HasFormContentType)
            {
                // Allocating a Task only when the body is form data.
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
                form = await request.ReadFormAsync();
                KeyValueAccumulator accumulator = default;
                foreach (var r in form) 
                {
                    foreach (var k in r.Value) 
                    {
                        var ct = AppInfo.Instance.Certificate.Decrypt(k);
                        accumulator.Append(r.Key, ct);
                    }
                }
                form = new FormCollection(accumulator.GetResults());
            }
            catch (InvalidDataException ex)
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
