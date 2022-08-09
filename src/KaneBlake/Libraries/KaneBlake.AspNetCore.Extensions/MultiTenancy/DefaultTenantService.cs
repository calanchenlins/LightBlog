using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.MultiTenancy
{
    public class DefaultTenantService<T> : ITenantService<T> where T : class
    {
        /// <summary>
        /// The current options for the <see cref="RequestLocalizationMiddleware"/>.
        /// </summary>
        public MultiTenancyOptions<T> Options { get; set; }

        public Task<TenantInfo<T>> GetTenantInfoAsync(T tenantId)
        {
            TenantInfo<T> tenantInfo = default;
            var tenantConfiguration = Options.TenantConfiguration?.FirstOrDefault(t => tenantId.Equals(t.Id));
            if (tenantConfiguration != null) 
            {
                tenantInfo = new TenantInfo<T>(tenantConfiguration?.Id, tenantConfiguration?.Name, tenantConfiguration?.ConnectionStrings);
            }
            return Task.FromResult(tenantInfo);
        }
    }
}
