using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LightBlog.Infrastruct.Context;
using LightBlog.Data;
using Autofac.Extensions.DependencyInjection;
using K.Basis.Common.Extensions;
using K.AspNetCore.Extensions.Hosting;
using Serilog;
using K.AspNetCore.Extensions;

namespace LightBlog
{
    public class Program
    {
        public static int Main(string[] args) => Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            }).UseSerilog().Build()
            .MigrateDbContext<UserDbContext>((_, __) => { DataSeeder.UserDbSeeder(_); })
            .MigrateDbContext<PostDbContext>((_, __) => { DataSeeder.PostDbSeeder(_); })
            .RunWebHost();

    }
}
