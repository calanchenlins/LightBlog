using KaneBlake.AspNetCore.Extensions.ConnectedServices;
using KaneBlake.AspNetCore.Extensions.MultiTenancy;
using KaneBlake.AspNetCore.Extensions.MVC.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions
{
    public static class CustomServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <TypedJsonTempDataSerializer> as the default <TempDataSerializer> in the<IServiceCollection>.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddTypedJsonTempDataSerializer(this IServiceCollection services)
        {
            var descriptor = ServiceDescriptor.Singleton(typeof(TempDataSerializer), typeof(TypedJsonTempDataSerializer));
            return services.Replace(descriptor);
        }

        /// <summary>
        ///  Adds services required for using WcfClient.
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
        ///  Adds services required for using WcfClient.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
        {
            //services.Configure<MultiTenancyOptions<string>>(Configuration.GetSection("MultiTenancy"));
            return services;
        }
    }
}
