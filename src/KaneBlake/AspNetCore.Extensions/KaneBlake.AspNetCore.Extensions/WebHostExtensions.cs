using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions
{
    public static class WebHostExtensions
    {
        /// <summary>
        /// Configure serilog, Runs an application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static int RunWebHost(this IHost host)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("WebHost Starting.");
                host.Run();
                return 0;
            }
            catch (OperationCanceledException)
            {
                Log.Information("WebHost Stopped.");
                return 1;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "WebHost Terminated Unexpectedly.");
                // Set exit code: The return value from Main is treated as the exit code for the process. 
                // Returning an integer enables the program to communicate status information to other programs or scripts that invoke the executable file. 
                // If void is returned from Main, the exit code will be implicitly 0. 
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
