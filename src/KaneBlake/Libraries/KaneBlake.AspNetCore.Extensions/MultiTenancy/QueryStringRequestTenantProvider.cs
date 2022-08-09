using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.MultiTenancy
{
    /// <summary>
    /// Determines the tenant information for a request via values in the query string.
    /// </summary>
    public class QueryStringRequestTenantProvider<T> : IRequestTenantProvider<T> where T : class
    {
        /// <summary>
        /// The current options for the <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        public MultiTenancyOptions<T> Options { get; set; }

        /// <summary>
        /// The key that contains the tenant Id.
        /// Defaults to "tenantId".
        /// </summary>
        public string QueryStringKey { get; set; } = "tenantId";


        /// <inheritdoc />
        public Task<ProviderTenantResult<T>> DetermineProviderTenantResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var request = httpContext.Request;

            if (!request.QueryString.HasValue)
            {
                return Task.FromResult(default(ProviderTenantResult<T>));
            }

            string queryTenant = request.Query[QueryStringKey];

            if (string.IsNullOrWhiteSpace(queryTenant))
            {
                return Task.FromResult(default(ProviderTenantResult<T>));
            }

            T tenantId = default;

            var typeConverter = TypeDescriptor.GetConverter(typeof(T));

            if (typeConverter.CanConvertFrom(typeof(string)))
            {
                tenantId = (T)typeConverter.ConvertFromString(queryTenant);
            }

            return Task.FromResult(new ProviderTenantResult<T>(tenantId));
        }
    }
}
