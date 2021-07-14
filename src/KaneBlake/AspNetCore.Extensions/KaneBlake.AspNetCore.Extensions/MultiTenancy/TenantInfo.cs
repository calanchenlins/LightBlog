using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace KaneBlake.AspNetCore.Extensions.MultiTenancy
{
    /// <summary>
    /// This class includes information of a tenant. 
    /// You should create a new instance and assign it to <see cref="CurrentTenant"/> 
    /// when you need change the settings of <see cref="CurrentTenant"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class TenantInfo<T> where T : class
    {
        private readonly Dictionary<string, string> _connectionStrings;


        public T TenantId { get; }

        public string Name { get; }

        public string this[string key] { get => _connectionStrings?.GetValueOrDefault(key); }


        public TenantInfo(T tenantId, string name = null, Dictionary<string, string> connectionStrings = null)
        {
            TenantId = tenantId;
            Name = name;
            _connectionStrings = connectionStrings;
        }



        private static AsyncLocal<TenantInfo<T>> s_asyncLocalCurrentTenant;

        /// <summary>
        /// Get current htttp request's <see cref="TenantInfo{T}"/>.
        /// This value are volatile and may change over the lifetime of thethread.
        /// </summary>
        public static TenantInfo<T> CurrentTenant
        {
            get
            {
                return s_asyncLocalCurrentTenant?.Value;
            }
            set
            {
                if (s_asyncLocalCurrentTenant == null)
                {
                    Interlocked.CompareExchange(ref s_asyncLocalCurrentTenant, new AsyncLocal<TenantInfo<T>>(), null);
                }
                s_asyncLocalCurrentTenant.Value = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

    }
}
