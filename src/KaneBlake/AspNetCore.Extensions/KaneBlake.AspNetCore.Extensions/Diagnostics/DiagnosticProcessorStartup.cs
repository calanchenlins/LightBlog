using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using KaneBlake.Basis.Extensions.Diagnostics;

[assembly: HostingStartup(typeof(KaneBlake.AspNetCore.Extensions.Diagnostics.DiagnosticProcessorStartup))]
namespace KaneBlake.AspNetCore.Extensions.Diagnostics
{
    public class DiagnosticProcessorStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services => {
                services.AddSingleton<IHostedService, DiagnosticProcessorService>();
                services.AddSingleton<DiagnosticAdapterProcessorObserver>();
            });
        }
    }
}
