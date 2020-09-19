using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using KaneBlake.Basis.Common.Diagnostics;
using KaneBlake.Basis.Services;

[assembly: HostingStartup(typeof(KaneBlake.AspNetCore.Extensions.Hosting.ExtensionServiceHostingStartup))]
namespace KaneBlake.AspNetCore.Extensions.Hosting
{
    public class ExtensionServiceHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                services.AddSingleton<IHostedService, InstrumentationHostedService>();
                services.AddSingleton<IExecutionService,DiagnosticProcessorService>();
                services.AddSingleton<DiagnosticAdapterProcessorObserver>();
            });
        }
    }
}
