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
                services.AddAutoInjectionService();
                services.AddApplicationServiceHandler(context.Configuration.GetSection("ApplicationServices"));
                services.AddSingleton<IHostedService, InstrumentationHostedService>();
                services.AddSingleton<IExecutionService, DiagnosticProcessorService>();
                services.AddSingleton<DiagnosticAdapterProcessorObserver>();
            });
        }
    }
}
