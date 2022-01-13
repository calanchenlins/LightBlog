using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions.MultiTenancy
{
    public class ProviderTenantResult<T> where T : class
    {
        public T TenantId { get; }

        public ProviderTenantResult(T tenantId)
        {
            TenantId = tenantId;
        }
    }

    /// <summary>
    /// Represents a provider for determining the tenant information of an <see cref="HttpRequest"/>.
    /// </summary>
    public interface IRequestTenantProvider<T> where T : class
    {
        /// <summary>
        /// Implements the provider to determine the tenant of the given request.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> for the request.</param>
        /// <returns>
        ///     The determined tenant id<see cref="{T}"/>.
        ///     Returns <c>null</c> if the provider couldn't determine a <see cref="TenantInfo{T}"/>.
        /// </returns>
        Task<ProviderTenantResult<T>> DetermineProviderTenantResult(HttpContext httpContext);
    }
}
