using KaneBlake.AspNetCore.Extensions.ConnectedServices;
using KaneBlake.AspNetCore.Extensions.MultiTenancy;
using KaneBlake.AspNetCore.Extensions.MVC.ViewFeatures;
using KaneBlake.AspNetCore.Extensions.Services.Module;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="TypedJsonTempDataSerializer"/> as the default <see cref="TempDataSerializer"/> implication in the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddTypedJsonTempDataSerializer(this IServiceCollection services)
        {
            var descriptor = ServiceDescriptor.Singleton(typeof(TempDataSerializer), typeof(TypedJsonTempDataSerializer));
            return services.Replace(descriptor);
        }

        /// <summary>
        /// Adds services required for using WcfClient.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddWcfClient(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddSingleton<IWcfClientFactory, WcfClientFactory>();
            services.AddSingleton(typeof(IWcfClient<,>), typeof(WcfClientManager<,>));
            return services;
        }


        /// <summary>
        /// Adds services required for using ApplicationServiceHandler.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddApplicationServiceHandler(this IServiceCollection services, IConfiguration configuration)
        {
            ///  Register applicationServices in appsettings.json: 
            /// "ApplicationServices": {
            ///   "Services": {
            ///     "SCPGSubmit": {
            ///       "Components": [
            ///         {
            ///           "Name": "PES.CG.WebApi.Services.SCPGSubmitComponent1"
            ///         },
            ///         {
            ///           "Name": "PES.CG.WebApi.Services.SCPGSubmitComponent2"
            ///         }
            ///       ]
            ///     }
            ///   }
            /// }

            services.AddScoped<IApplicationServiceClient, ApplicationServiceClient>();
            services.AddSingleton<ApplicationServiceCacheEntryResolver>();
            services.Configure<ApplicationServiceOptions>(configuration);
            return services;
        }


        public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
        {
            //services.Configure<MultiTenancyOptions<string>>(Configuration.GetSection("MultiTenancy"));
            return services;
        }


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
                            services.Add(new ServiceDescriptor(injectionService.implementationType, injectionService.implementationType, attr.Lifetime));
                        }
                        else
                        {
                            foreach (var serviceType in attr.ServiceTypes)
                            {
                                services.Add(new ServiceDescriptor(serviceType, injectionService.implementationType, attr.Lifetime));
                            }
                        }
                    }
                }
            }
            return services;
        }
    }
}
