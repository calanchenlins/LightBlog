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

namespace KaneBlake.STS.Identity.Quickstart
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EncryptFormFilterAttribute : Attribute, IResourceFilter
    {
        private readonly EncryptFormValueProviderFactory _valueProviderFactory;

        public EncryptFormFilterAttribute(EncryptFormValueProviderFactory valueProviderFactory)
        {
            _valueProviderFactory = valueProviderFactory ?? throw new ArgumentNullException(nameof(valueProviderFactory));

        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var _ = context.HttpContext.Request.Headers.FirstOrDefault(h => "Content-Type".Equals(h.Key)&& h.Value.Any(v=> "application/x-www-form-urlencoded".Equals(v)));
            var __ = context.HttpContext.Request.Headers.FirstOrDefault(h => "form-encrypt".Equals(h.Key) && h.Value.Any(v => "formrsaencrypt".Equals(v)));

            var formValueProviderFactory = context.ValueProviderFactories
                .OfType<FormValueProviderFactory>()
                .FirstOrDefault();
            if (formValueProviderFactory != null)
            {
                context.ValueProviderFactories.Remove(formValueProviderFactory);
            }

            if (!(_.Key is null || __.Key is null)) 
            {
                var encryptFormValueProviderFactory = context.ValueProviderFactories
                    .OfType<EncryptFormValueProviderFactory>()
                    .FirstOrDefault();
                if (encryptFormValueProviderFactory == null)
                {
                    // 默认按照顺序查找value
                    context.ValueProviderFactories.Insert(0, _valueProviderFactory);
                }
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
