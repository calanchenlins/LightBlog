using K.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace K.AspNetCore.Extensions.MultiTenancy
{
    /// <summary>
    /// tenant entity that map to table in database
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Tenant<T> : Entity<T>
    {
        /// <summary>
        /// Name of the tenant
        /// </summary>
        [Required]
        public string TenantName { get; private set; }

        public ConnectionStrings ConnectionStrings { get; private set; }
    }


    public class ConnectionStrings : Dictionary<string, string>
    {
        public const string DefaultConnectionStringName = "Default";

        public string Default
        {
            get => this.GetValueOrDefault(DefaultConnectionStringName);
            set => this[DefaultConnectionStringName] = value;
        }
    }
}
