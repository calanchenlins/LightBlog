using LightBlog.Services.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LightBlog.Common.Diagnostics
{
    /// <summary>
    /// DiagnosticListener订阅过滤器
    /// 单例模式
    /// </summary>
    public class DiagnosticProcessorObserver : IObserver<DiagnosticListener>
    {

        private readonly IHomeCacheService _homeCacheService;

        public DiagnosticProcessorObserver(IHomeCacheService homeCacheService)
        {
            _homeCacheService = homeCacheService;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DiagnosticListenerName.PostServiceDiagnosticListenerName)
            {
                listener.Subscribe(new HomeCacheChangeObserver(_homeCacheService));
            }
        }
    }
}
