using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using K.AspNetCore.Extensions.Hosting;
using KaneBlake.STS.Identity.Infrastruct;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using K.AspNetCore.Extensions;

namespace KaneBlake.STS.Identity
{
    public class Program
    {
        public static int Main(string[] args) => Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            }).UseSerilog().Build()
            .MigrateDbContext<Infrastruct.Context.UserDbContext>((_, __) => { })
            //.MigrateDbContext<ConfigurationDbContext>((_, __) => { DataSeeder.Seed(_); })
            .RunWebHost();
    }
}
