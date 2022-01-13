using KaneBlake.Extensions.Diagnostics;
using KaneBlake.Extensions.Diagnostics.Abstractions;
using LightBlog.Infrastruct.Entities;
using LightBlog.Services.Cache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using LightBlogEvents = LightBlog.Common.Diagnostics.LightBlogDiagnosticListenerExtensions;

namespace LightBlog.Common.Diagnostics
{
    class HomeCacheDiagnosticProcessor : IDiagnosticProcessor
    {
        private readonly IHomeCacheService _homeCacheService;

        public HomeCacheDiagnosticProcessor(IHomeCacheService homeCacheService)
        {
            _homeCacheService = homeCacheService;
        }

        public string ListenerName => LightBlogEvents.DiagnosticListenerName;


        [DiagnosticAdapterName(LightBlogEvents.AfterAddOrUpdatePost)]
        public void OnAddOrUpdate([Object]Post post)
        {
            _homeCacheService.AddOrUpdate(post);
        }

        [DiagnosticAdapterName(LightBlogEvents.AfterDeletePost)]
        public void OnDelete([Object] int postId)
        {
            _homeCacheService.Delete(postId);
        }

        [DiagnosticAdapterName(LightBlogEvents.AfterPostAddComment)]
        public void OnAddComment([Object]int postId)
        {
            _homeCacheService.AddComment(postId);
        }
    }
}
