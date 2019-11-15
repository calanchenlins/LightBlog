using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using KaneBlake.Basis.Diagnostics;

[assembly: HostingStartup(typeof(KaneBlake.Basis.Startup.HostedServiceStartup))]
namespace KaneBlake.Basis.Startup
{
    public class HostedServiceStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services=> {
                services.AddSingleton<IHostedService, InstrumentationHostedService>();
                services.AddSingleton<IInstrumentStartup, InstrumentStartup>();
                services.AddSingleton<DiagnosticAdapterProcessorObserver>();
            });
        }
    }
}
