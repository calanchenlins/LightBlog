using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace K.Infrastruct.EntityFrameworkCore
{
    /// <summary>
    /// 领域聚合根上下文对象
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TSelf"></typeparam>
    public abstract class TEntityContext<TEntity, TSelf> : DbContext where TEntity : class where TSelf : TEntityContext<TEntity, TSelf>
    {
        private readonly ILoggerFactory _loggerFactory;

        protected TEntityContext(DbContextOptions options,ILoggerFactory loggerFactory) : base(options)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //配置代理延迟加载
            //延迟加载:依赖 Nuget包 Microsoft.EntityFrameworkCore.Proxies:在访问导航属性时，从数据库中以透明方式加载关联数据
            optionsBuilder.UseLazyLoadingProxies();
            optionsBuilder.UseLoggerFactory(_loggerFactory);
            base.OnConfiguring(optionsBuilder);
        }

    }
}
