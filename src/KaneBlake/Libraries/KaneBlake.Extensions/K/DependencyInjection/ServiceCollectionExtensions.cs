using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace K.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Inject services with <see cref="AutoInjectionAttribute"/>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddAutoInjectionService(this IServiceCollection services)
        {
            var assemblyServices = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetExportedTypes().Where(t => !t.IsInterface && !t.IsAbstract)
                .Select(t => new { implementationType = t, autoInjectionAttributes = t.GetCustomAttributes<AutoInjectionAttribute>() })
                .Where(r => r.autoInjectionAttributes.Any()));

            foreach (var injectionServices in assemblyServices)
            {
                foreach (var injectionService in injectionServices)
                {
                    foreach (var attr in injectionService.autoInjectionAttributes)
                    {
                        if (attr.ServiceTypes.Length <= 0)
                        {
                            services.TryAdd(new ServiceDescriptor(injectionService.implementationType, injectionService.implementationType, attr.Lifetime));
                        }
                        else
                        {
                            foreach (var serviceType in attr.ServiceTypes)
                            {
                                services.TryAddEnumerable(new ServiceDescriptor(serviceType, injectionService.implementationType, attr.Lifetime));
                            }
                        }
                    }
                }
            }
            return services;
        }
    }
}
