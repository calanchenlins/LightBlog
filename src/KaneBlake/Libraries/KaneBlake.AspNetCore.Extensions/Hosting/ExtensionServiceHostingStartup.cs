﻿using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using K.AspNetCore.Extensions.Hosting;
using System.Linq;
using System.Reflection;
using K.AspNetCore.Extensions.DependencyInjection;
using K.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using K.AspNetCore.Extensions.MVC;
using K.Serialization;
using K.Extensions;

[assembly: HostingStartup(typeof(ExtensionServiceHostingStartup))]
namespace K.AspNetCore.Extensions.Hosting
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
