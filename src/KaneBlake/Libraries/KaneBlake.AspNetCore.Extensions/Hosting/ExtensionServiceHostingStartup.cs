using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using KaneBlake.AspNetCore.Extensions.Hosting;
using System.Linq;
using System.Reflection;
using KaneBlake.AspNetCore.Extensions.DependencyInjection;
using K.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using KaneBlake.AspNetCore.Extensions.MVC;
using K.Basis.Common.Serialization;

[assembly: HostingStartup(typeof(ExtensionServiceHostingStartup))]
namespace KaneBlake.AspNetCore.Extensions.Hosting
{
    public class ExtensionServiceHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {

                services.Configure<MvcOptions>(options => options.Configure());
                services.Configure<ApiBehaviorOptions>(options => options.Configure());
                services.Configure<JsonOptions>(options => options.JsonSerializerOptions.Configure());

                services.AddApplicationServiceHandler(context.Configuration.GetSection("AppOptions"));

                services.AddDiagnosticProcessor(context.Configuration.GetSection("AppOptions"));

                services.AddAutoInjectionService();
            });
        }
    }
}
