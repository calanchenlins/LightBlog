using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using K.AspNetCore.Extensions.MVC.Module.ApplicationParts;

namespace K.AspNetCore.Extensions.MVC.Module
{
    public static class MvcRazorLoggerExtensions
    {
        private static readonly Action<ILogger, string, Exception> _viewCompilerLocatedCompiledView = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "ViewCompilerLocatedCompiledView"), "Initializing Razor view compiler with compiled view: '{ViewName}'.");

        private static readonly Action<ILogger, Exception> _viewCompilerNoCompiledViewsFound = LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ViewCompilerNoCompiledViewsFound"), "Initializing Razor view compiler with no compiled views.");

        private static readonly Action<ILogger, string, Exception> _viewCompilerLocatedCompiledViewForPath = LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "ViewCompilerLocatedCompiledViewForPath"), "Located compiled view for view at path '{Path}'.");

        private static readonly Action<ILogger, string, Exception> _viewCompilerCouldNotFindFileToCompileForPath = LoggerMessage.Define<string>(LogLevel.Trace, new EventId(7, "ViewCompilerCouldNotFindFileAtPath"), "Could not find a file for view at path '{Path}'.");

        public static void ViewCompilerLocatedCompiledView(this ILogger logger, string view)
        {
            _viewCompilerLocatedCompiledView(logger, view, null);
        }
        public static void ViewCompilerNoCompiledViewsFound(this ILogger logger)
        {
            _viewCompilerNoCompiledViewsFound(logger, null);
        }
        public static void ViewCompilerLocatedCompiledViewForPath(this ILogger logger, string path)
        {
            MvcRazorLoggerExtensions._viewCompilerLocatedCompiledViewForPath(logger, path, null);
        }
        public static void ViewCompilerCouldNotFindFileAtPath(this ILogger logger, string path)
        {
            MvcRazorLoggerExtensions._viewCompilerCouldNotFindFileToCompileForPath(logger, path, null);
        }
    }

    public class ModuleViewCompilerProvider : IViewCompilerProvider
    {
        private readonly ModuleViewCompiler _compiler;

        public ModuleViewCompilerProvider(
            ApplicationPartManager applicationPartManager,
            IModuleChangeProvider moduleChangeProvider,
            ILoggerFactory loggerFactory)
        {
            _compiler = new ModuleViewCompiler(applicationPartManager, moduleChangeProvider, loggerFactory.CreateLogger<ModuleViewCompiler>());
        }

        public IViewCompiler GetCompiler() => _compiler;
    }

    public class ModuleViewCompiler : IViewCompiler
    {
        private readonly object _cacheLock = new object();

        private readonly ApplicationPartManager _partManager;

        private readonly IModuleChangeProvider _moduleChangeProvider;

        private readonly Dictionary<string, Task<CompiledViewDescriptor>> _compiledViews;

        private readonly ConcurrentDictionary<string, string> _normalizedPathCache;

        private readonly IMemoryCache _cache;

        private readonly ILogger _logger;

        public ModuleViewCompiler(
            ApplicationPartManager partManager,
            IModuleChangeProvider moduleChangeProvider,
            ILogger logger)
        {
            _partManager = partManager ?? throw new ArgumentNullException(nameof(partManager));
            _moduleChangeProvider = moduleChangeProvider ?? throw new ArgumentNullException(nameof(moduleChangeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _normalizedPathCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

            // This is our L0 cache, and is a durable store. Views migrate into the cache as they are requested
            // from either the set of known precompiled views, or by being compiled.
            _cache = new MemoryCache(new MemoryCacheOptions());

            var moduleRazorParts = _partManager.ApplicationParts.OfType<CompiledRazorModulesAssemblyPart>().ToList();

            var razorParts = _partManager.ApplicationParts.OfType<CompiledRazorAssemblyPart>().ToList();

            _compiledViews = new Dictionary<string, Task<CompiledViewDescriptor>>(StringComparer.OrdinalIgnoreCase);
            var precompiledViews = new Dictionary<string, CompiledViewDescriptor>(StringComparer.OrdinalIgnoreCase);

            foreach (var razorPart in moduleRazorParts)
            {
                if (razorPart is IRazorCompiledItemProvider provider)
                {

                    foreach (var item in provider.CompiledItems)
                    {
                        var descriptor = new CompiledViewDescriptor(item)
                        {
                            ExpirationTokens = new List<IChangeToken>() { _moduleChangeProvider.GetChangeToken(razorPart.EntryAssemblyPath) }
                        };
                        if (!precompiledViews.ContainsKey(descriptor.RelativePath))
                        {
                            precompiledViews.Add(descriptor.RelativePath, descriptor);
                        }
                    }
                }
            }
            foreach (var razorPart in razorParts)
            {
                if (razorPart is IRazorCompiledItemProvider provider)
                {

                    foreach (var item in provider.CompiledItems)
                    {
                        var descriptor = new CompiledViewDescriptor(item);
                        if (!_compiledViews.ContainsKey(descriptor.RelativePath))
                        {
                            _compiledViews.Add(descriptor.RelativePath, Task.FromResult(descriptor));
                        }
                    }
                }
            }
            foreach (var razorView in precompiledViews)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions();

                for (var i = 0; i < razorView.Value.ExpirationTokens.Count; i++)
                {
                    cacheEntryOptions.ExpirationTokens.Add(razorView.Value.ExpirationTokens[i]);
                }
                _cache.Set(razorView.Value.RelativePath, Task.FromResult(razorView), cacheEntryOptions);
            }
        }

        public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (_compiledViews.TryGetValue(relativePath, out Task<CompiledViewDescriptor> cachedResult))
            {
                _logger.ViewCompilerLocatedCompiledViewForPath(relativePath);
                return cachedResult;
            }
            var normalizedPath = GetNormalizedPath(relativePath);
            if (_compiledViews.TryGetValue(normalizedPath, out cachedResult))
            {
                _logger.ViewCompilerLocatedCompiledViewForPath(normalizedPath);
                return cachedResult;
            }

            if (_cache.TryGetValue(normalizedPath, out cachedResult))
            {
                return cachedResult;
            }

            // Entry does not exist. Attempt to create one.
            cachedResult = OnCacheMiss(normalizedPath);
            return cachedResult;
        }


        private Task<CompiledViewDescriptor> OnCacheMiss(string normalizedPath)
        {
            TaskCompletionSource<CompiledViewDescriptor> taskSource;
            lock (_cacheLock)
            {
                // Double-checked locking to handle a possible race.
                if (_cache.TryGetValue(normalizedPath, out Task<CompiledViewDescriptor> result))
                {
                    return result;
                }
                var razorParts = _partManager.ApplicationParts.OfType<CompiledRazorModulesAssemblyPart>().ToList();
                foreach (var razorPart in razorParts)
                {
                    var provider = razorPart as IRazorCompiledItemProvider;
                    var razorCompiledItems = provider.CompiledItems;
                    var item = razorCompiledItems.FirstOrDefault(item => normalizedPath.Equals(GetNormalizedPath(item.Identifier)));
                    if (item != null)
                    {
                        taskSource = new TaskCompletionSource<CompiledViewDescriptor>(creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);

                        var descriptor = new CompiledViewDescriptor(item)
                        {
                            ExpirationTokens = new List<IChangeToken>() { _moduleChangeProvider.GetChangeToken(razorPart.EntryAssemblyPath) }
                        };

                        // At this point, we've decided what to do - but we should create the cache entry and
                        // release the lock first.
                        var cacheEntryOptions = new MemoryCacheEntryOptions();

                        for (var i = 0; i < descriptor.ExpirationTokens.Count; i++)
                        {
                            cacheEntryOptions.ExpirationTokens.Add(descriptor.ExpirationTokens[i]);
                        }
                        var task = _cache.Set(descriptor.RelativePath, Task.FromResult(descriptor), cacheEntryOptions);
                        return task;
                    }
                }
            }

            // Entry does not exist. Attempt to create one.
            _logger.ViewCompilerCouldNotFindFileAtPath(normalizedPath);
            return Task.FromResult(new CompiledViewDescriptor
            {
                RelativePath = normalizedPath,
                ExpirationTokens = Array.Empty<IChangeToken>(),
            });
        }

        private string GetNormalizedPath(string relativePath)
        {
            Debug.Assert(relativePath != null);
            if (relativePath.Length == 0)
            {
                return relativePath;
            }

            if (!_normalizedPathCache.TryGetValue(relativePath, out var normalizedPath))
            {
                normalizedPath = NormalizePath(relativePath);
                _normalizedPathCache[relativePath] = normalizedPath;
            }

            return normalizedPath;
        }
        public static string NormalizePath(string path)
        {
            var addLeadingSlash = path[0] != '\\' && path[0] != '/';
            var transformSlashes = path.IndexOf('\\') != -1;

            if (!addLeadingSlash && !transformSlashes)
            {
                return path;
            }

            var length = path.Length;
            if (addLeadingSlash)
            {
                length++;
            }

            return string.Create(length, (path, addLeadingSlash), (span, tuple) =>
            {
                var (pathValue, addLeadingSlashValue) = tuple;
                var spanIndex = 0;

                if (addLeadingSlashValue)
                {
                    span[spanIndex++] = '/';
                }

                foreach (var ch in pathValue)
                {
                    span[spanIndex++] = ch == '\\' ? '/' : ch;
                }
            });
        }

    }
}
