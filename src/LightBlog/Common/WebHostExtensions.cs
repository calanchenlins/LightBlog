using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LightBlog.Common
{
    public static class WebHostExtensions
    {
        /// <summary>
        /// 迁移DbContext,并写入初始种子数据
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="webHost"></param>
        /// <param name="seeder"></param>
        /// <returns></returns>
        public static IHost MigrateDbContext<TContext>(this IHost webHost, Action<TContext, IServiceProvider> seeder) where TContext : DbContext
        {
            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var env = services.GetRequiredService<IWebHostEnvironment>();

                if (env.IsDevelopment())
                {
                    var logger = services.GetRequiredService<ILogger<TContext>>();

                    var context = services.GetService<TContext>();
                    if (context.Database.GetPendingMigrations().Any())
                    {
                        try
                        {
                            logger.LogInformation($"Migrating database associated with context {typeof(TContext).Name}");
                            //重试器
                            //SqlException:仅处理sql执行错误，不包括连接失败

                            var retry = Policy.Handle<SqlException>()
                                 .WaitAndRetry(new TimeSpan[]
                                 {
                             TimeSpan.FromSeconds(3),
                             TimeSpan.FromSeconds(5),
                             TimeSpan.FromSeconds(8),
                                 });

                            retry.Execute(() =>
                            {
                                // 应用迁移记录到数据库
                                context.Database.Migrate();
                                // 填充初始数据
                                seeder(context, services);
                            });
                            logger.LogInformation($"Migrated database associated with context {typeof(TContext).Name}");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, $"An error occurred while migrating the database used on context {typeof(TContext).Name}");
                        }
                    }
                }
            }
            return webHost;
        }
    }
}
