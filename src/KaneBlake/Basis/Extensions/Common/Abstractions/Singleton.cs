using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace KaneBlake.Basis.Extensions.Common.Abstractions
{
    /// <summary>
    /// 使用Lazy<T>实现单例模式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Singleton<T> where T : new()
    {
        //静态字段是线程安全的
        //实现单例模式的重点：对静态字段只能初始化一次
        static readonly Lazy<T> lazyT;

        //静态构造函数：初始化所有静态成员（只会被执行一次）
        //默认Lazy<T>构造函数使用ExecutionAndPublication双检锁技术
        //PublicationOnly:通过CompareExchange实现
        //T的构造函数只能是Func<T>
        static Singleton() => lazyT = new Lazy<T>(() => { return new T(); }, LazyThreadSafetyMode.PublicationOnly);

        //通过Lazy的懒加载实现延迟访问
        public static T Instance => lazyT.Value;
    }
}
