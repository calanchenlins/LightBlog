using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.MultiTenancy
{
    /// <summary>
    /// Specifies options for the <see cref="MultiTenancyMiddleware{T}"/>.
    /// </summary>
    public class MultiTenancyOptions<T> where T : class
    {
        public List<TenantConfiguration<T>> TenantConfiguration { get; set; }

        public string MyProperty { get; private set; }

        /// <summary>
        /// Creates a new <see cref="MultiTenancyOptions"/> with default values.
        /// </summary>
        public MultiTenancyOptions()
        {
            RequestTenantProviders = new List<IRequestTenantProvider<T>>
            {
                new QueryStringRequestTenantProvider<T>(){ Options = this }
            };
            TenantService = new DefaultTenantService<T>() { Options = this };
        }

        /// <summary>
        /// An ordered list of providers used to determine a request's tenant information. The first provider that
        /// returns a non-<c>null</c> result for a given request will be used.
        /// Defaults to the following:
        /// <list type="number">
        ///     <item><description><see cref="QueryStringRequestTenantProvider{T}"/></description></item>
        /// </list>
        /// </summary>
        public IList<IRequestTenantProvider<T>> RequestTenantProviders { get; set; }

        public ITenantService<T> TenantService { get; set; }
    }
}
