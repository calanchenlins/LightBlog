using System;
using System.Collections.Generic;
using System.Text;

namespace K.AspNetCore.Extensions.MultiTenancy
{
    public class TenantConfiguration<T>
    {
        /// <summary>
        /// public set accessor is required in configuration
        /// </summary>
        public T Id { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> ConnectionStrings { get; set; }

        /// <summary>
        /// public parameterless constructor is required in <see cref="Microsoft.Extensions.Configuration.ConfigurationBinder"/>
        /// </summary>
        public TenantConfiguration()
        {
        }
    }
}
