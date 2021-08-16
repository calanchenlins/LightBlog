using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using KaneBlake.Basis.Common.Diagnostics;
using KaneBlake.Basis.Services;
using KaneBlake.AspNetCore.Extensions.Hosting;
using KaneBlake.AspNetCore.Extensions.Services;
using System.Linq;
using System.Reflection;
using KaneBlake.AspNetCore.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(ExtensionServiceHostingStartup))]
namespace KaneBlake.AspNetCore.Extensions.Hosting
{
    public class ExtensionServiceHostingStartup : IHostingStartup
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {

                // Inject services with AutoInjectionAttribute
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


                services.AddSingleton<ApplicationServiceEntryResolver>();
                services.Configure<ApplicationServiceOptions>(context.Configuration.GetSection("ApplicationServices"));
                services.AddScoped<ApplicationService>();

                services.AddSingleton<IHostedService, InstrumentationHostedService>();
                services.AddSingleton<IExecutionService, DiagnosticProcessorService>();
                services.AddSingleton<DiagnosticAdapterProcessorObserver>();
            });
        }
    }
}
