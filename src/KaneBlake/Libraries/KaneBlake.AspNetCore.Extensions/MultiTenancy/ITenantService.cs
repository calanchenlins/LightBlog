using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace K.AspNetCore.Extensions.MultiTenancy
{
    public interface ITenantService<T> where T : class
    {
        Task<TenantInfo<T>> GetTenantInfoAsync(T tenantId);
    }
}
