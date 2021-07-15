using KaneBlake.AspNetCore.Extensions.MultiTenancy;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions
{
    public class AppOptions
    {
        public Dictionary<string, string> ConnectionStrings { get; set; }


        /// <summary>
        /// Resolve connectionString from <see cref="TenantInfo{T}"/> and <see cref="ConnectionStrings"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ResolveConnectionString<T>(string key) where T : class
        {
            var currentTenant = TenantInfo<T>.CurrentTenant;

            if (currentTenant !=null && currentTenant.TryGetConnectionString(key, out var tenantConnStr) 
                && !string.IsNullOrEmpty(tenantConnStr))
            {
                return tenantConnStr;
            }
            else if(ConnectionStrings.TryGetValue(key, out var connStr) && !string.IsNullOrEmpty(connStr))
            {
                return connStr;
            }

            return string.Empty;
        }
    }
}
