﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LightBlog.Common.Logging;
using LightBlog.Common;
using LightBlog.Infrastruct.Context;
using LightBlog.Data;
using Autofac.Extensions.DependencyInjection;

namespace LightBlog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build()
                .MigrateDbContext<UserDbContext>((_, __) => { DataSeeder.UserDbSeeder(_); })
                .MigrateDbContext<PostDbContext>((_, __) => { DataSeeder.PostDbSeeder(_); })
                .Run();
        }

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>()
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddFile(hostingContext.Configuration);
                });
            });

    }
}
