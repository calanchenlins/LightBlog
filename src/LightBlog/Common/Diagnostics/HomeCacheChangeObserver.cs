using LightBlog.Infrastruct.Entities;
using LightBlog.Services.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;

namespace LightBlog.Common.Diagnostics
{
    class HomeCacheChangeObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly IHomeCacheService _homeCacheService;

        public HomeCacheChangeObserver(IHomeCacheService homeCacheService)
        {
            _homeCacheService = homeCacheService;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> evt)
        {
            if (evt.Key == "AddOrUpdate")
            {
                _homeCacheService.AddOrUpdate((Post)evt.Value);
            }
            if (evt.Key == "Delete")
            {
                _homeCacheService.Delete((int)evt.Value);
            }
            if (evt.Key == "AddComment")
            {
                _homeCacheService.AddComment((int)evt.Value);
            }
        }
    }
}
