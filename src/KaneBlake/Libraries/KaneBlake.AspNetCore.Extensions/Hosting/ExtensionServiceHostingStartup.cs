using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using KaneBlake.AspNetCore.Extensions.Hosting;
using System.Linq;
using System.Reflection;
using KaneBlake.AspNetCore.Extensions.DependencyInjection;
using KaneBlake.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(ExtensionServiceHostingStartup))]
namespace KaneBlake.AspNetCore.Extensions.Hosting
{
    public class ExtensionServiceHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddApplicationServiceHandler(context.Configuration.GetSection("ApplicationServices"));

                services.AddDiagnosticProcessor(context.Configuration.GetSection("AppOptions"));

                services.AddAutoInjectionService();
            });
        }
    }
}
