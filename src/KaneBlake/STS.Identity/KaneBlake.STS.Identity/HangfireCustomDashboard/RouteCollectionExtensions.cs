using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.HangfireCustomDashboard
{
    /// <summary>
    /// https://github.com/yuzd/Hangfire.HttpJob/blob/master/Hangfire.HttpJob/Support/RouteCollectionExtensions.cs
    /// Provides extension methods for <see cref="RouteCollection"/>.
    /// </summary>
    internal static class RouteCollectionExtensions
    {
        private static readonly FieldInfo _dispatchers = typeof(RouteCollection).GetTypeInfo().GetDeclaredField(nameof(_dispatchers));

        /// <summary>
        /// Returns a private list of registered routes.
        /// </summary>
        /// <param name="routes">Route collection</param>
        private static List<Tuple<string, IDashboardDispatcher>> GetDispatchers(this RouteCollection routes)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));

            return (List<Tuple<string, IDashboardDispatcher>>)_dispatchers.GetValue(routes);
        }

        /// <summary>
        /// Checks if there's a dispatcher registered for given <paramref name="pathTemplate"/>.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        public static bool Contains(this RouteCollection routes, string pathTemplate)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));

            return routes.GetDispatchers().Any(x => x.Item1 == pathTemplate);
        }

        /// <summary>
        /// Combines exising dispatcher for <paramref name="pathTemplate"/> with <paramref name="dispatcher"/>.
        /// If there's no dispatcher for the specified path, adds a new one.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        /// <param name="dispatcher">Dispatcher to add or append for specified path</param>
        public static void Append(this RouteCollection routes, string pathTemplate, IDashboardDispatcher dispatcher)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            var list = routes.GetDispatchers();

            for (int i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                if (pair.Item1 == pathTemplate)
                {
                    var composite = pair.Item2 as CompositeDispatcher;
                    if (composite == null)
                    {
                        // replace original dispatcher with a composite one
                        composite = new CompositeDispatcher(pair.Item2);
                        list[i] = new Tuple<string, IDashboardDispatcher>(pathTemplate, composite);
                    }

                    composite.AddDispatcher(dispatcher);
                    return;
                }
            }

            routes.Add(pathTemplate, dispatcher);
        }

        /// <summary>
        /// Replaces exising dispatcher for <paramref name="pathTemplate"/> with <paramref name="dispatcher"/>.
        /// If there's no dispatcher for the specified path, adds a new one.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        /// <param name="dispatcher">Dispatcher to set for specified path</param>
        public static void Replace(this RouteCollection routes, string pathTemplate, IDashboardDispatcher dispatcher)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            var list = routes.GetDispatchers();

            for (int i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                if (pair.Item1 == pathTemplate)
                {
                    list[i] = new Tuple<string, IDashboardDispatcher>(pathTemplate, dispatcher);
                    return;
                }
            }

            routes.Add(pathTemplate, dispatcher);
        }

        /// <summary>
        /// Removes dispatcher for <paramref name="pathTemplate"/>.
        /// </summary>
        /// <param name="routes">Route collection</param>
        /// <param name="pathTemplate">Path template</param>
        public static void Remove(this RouteCollection routes, string pathTemplate)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));
            if (pathTemplate == null)
                throw new ArgumentNullException(nameof(pathTemplate));

            var list = routes.GetDispatchers();

            for (int i = 0; i < list.Count; i++)
            {
                var pair = list[i];
                if (pair.Item1 == pathTemplate)
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }
    }


    /// <summary>
    /// https://github.com/yuzd/Hangfire.HttpJob/blob/master/Hangfire.HttpJob/Support/CompositeDispatcher.cs
    /// Dispatcher that combines output from several other dispatchers.
    /// Used internally by <see cref="RouteCollectionExtensions.Append"/>.
    /// </summary>
    internal class CompositeDispatcher : IDashboardDispatcher
    {
        private readonly List<IDashboardDispatcher> _dispatchers;

        public CompositeDispatcher(params IDashboardDispatcher[] dispatchers)
        {
            _dispatchers = new List<IDashboardDispatcher>(dispatchers);
        }

        public void AddDispatcher(IDashboardDispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

            _dispatchers.Add(dispatcher);
        }

        public async Task Dispatch(DashboardContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (_dispatchers.Count == 0)
                throw new InvalidOperationException("CompositeDispatcher should contain at least one dispatcher");

            foreach (var dispatcher in _dispatchers)
            {
                await dispatcher.Dispatch(context);
            }
        }
    }

    internal class EmbeddedResourceDispatcher : IDashboardDispatcher
    {
        private readonly Assembly _assembly;
        private readonly string _resourceName;
        private readonly string _contentType;

        public EmbeddedResourceDispatcher(string contentType, Assembly assembly,string resourceName)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _resourceName = resourceName;
            _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }

        public async Task Dispatch(DashboardContext context)
        {
            if (!string.IsNullOrEmpty(_contentType))
            {
                var contentType = context.Response.ContentType;

                if (string.IsNullOrEmpty(contentType))
                {
                    // content type not yet set
                    context.Response.ContentType = _contentType;
                }
                else if (contentType != _contentType)
                {
                    // content type already set, but doesn't match ours
                    throw new InvalidOperationException($"ContentType '{_contentType}' conflicts with '{context.Response.ContentType}'");
                }
            }

            await WriteResponse(context.Response).ConfigureAwait(false);
        }

        protected virtual Task WriteResponse(DashboardResponse response)
        {
            return WriteResource(response, _assembly, _resourceName);
        }

        protected async Task WriteResource(DashboardResponse response, Assembly assembly, string resourceName)
        {
            using (var inputStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (inputStream == null)
                {
                    throw new ArgumentException($@"Resource with name {resourceName} not found in assembly {assembly}.");
                }

                await inputStream.CopyToAsync(response.Body).ConfigureAwait(false);
            }
        }
    }


    internal class StaticFileDispatcher : IDashboardDispatcher
    {
        private const int StreamCopyBufferSize = 64 * 1024;
        private readonly string _filePath;
        private readonly string _contentType;
        private readonly IFileProvider _fileProvider;

        public StaticFileDispatcher(string contentType, string filePath, IFileProvider fileProvider)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        public async Task Dispatch(DashboardContext context)
        {
            if (!string.IsNullOrEmpty(_contentType))
            {
                var contentType = context.Response.ContentType;

                if (string.IsNullOrEmpty(contentType))
                {
                    // content type not yet set
                    context.Response.ContentType = _contentType;
                }
                else if (contentType != _contentType)
                {
                    // content type already set, but doesn't match ours
                    throw new InvalidOperationException($"ContentType '{_contentType}' conflicts with '{context.Response.ContentType}'");
                }
            }

            await WriteResponse(context.Response).ConfigureAwait(false);
        }

        protected virtual Task WriteResponse(DashboardResponse response)
        {
            return WriteResource(response, _filePath);
        }

        protected async Task WriteResource(DashboardResponse response, string filePath)
        {
            var fileInfo = _fileProvider.GetFileInfo(filePath);
            if (!fileInfo.Exists) 
            {
                throw new ArgumentException($@"Could not find file '{fileInfo.PhysicalPath}'");
            }
            using (var readStream = fileInfo.CreateReadStream())
            {
                if (readStream == null)
                {
                    throw new ArgumentException($@"Resource with name {filePath} not found.");
                }
                // await readStream.CopyToAsync(response.Body).ConfigureAwait(false);
                // Larger StreamCopyBufferSize is required because in case of FileStream readStream isn't going to be buffering
                await StreamCopyOperation.CopyToAsync(readStream, response.Body, fileInfo.Length, StreamCopyBufferSize, CancellationToken.None);
            }
        }
    }

}
