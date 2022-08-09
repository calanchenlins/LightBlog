using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K.Extensions.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public class AutoInjectionAttribute : Attribute
    {
        public AutoInjectionAttribute() : this(ServiceLifetime.Scoped, Array.Empty<Type>())
        {
        }


        public AutoInjectionAttribute(ServiceLifetime lifetime) : this(lifetime, Array.Empty<Type>())
        {
        }


        public AutoInjectionAttribute(ServiceLifetime lifetime, params Type[] serviceTypes)
        {
            Lifetime = lifetime;
            ServiceTypes = serviceTypes ?? throw new ArgumentNullException(nameof(serviceTypes));
        }

        public ServiceLifetime Lifetime { get; } = ServiceLifetime.Scoped;

        public Type[] ServiceTypes { get; }

    }
}
