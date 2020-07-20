using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace KaneBlake.AspNetCore.Extensions
{
    public static class MvcRazorLoggerExtensions 
    {
        private static readonly Action<ILogger, string, Exception> _viewCompilerLocatedCompiledView = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(3, "ViewCompilerLocatedCompiledView"), "Initializing Razor view compiler with compiled view: '{ViewName}'.");

        private static readonly Action<ILogger, Exception> _viewCompilerNoCompiledViewsFound = LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ViewCompilerNoCompiledViewsFound"), "Initializing Razor view compiler with no compiled views.");
        public static void ViewCompilerLocatedCompiledView(this ILogger logger, string view)
        {
            _viewCompilerLocatedCompiledView(logger, view, null);
        }
        public static void ViewCompilerNoCompiledViewsFound(this ILogger logger)
        {
            _viewCompilerNoCompiledViewsFound(logger, null);
        }
    }
    public class DefaultViewCompiler : IViewCompiler
    {
        private readonly Dictionary<string, Task<CompiledViewDescriptor>> _compiledViews;
        private readonly ConcurrentDictionary<string, string> _normalizedPathCache;
        private readonly ILogger _logger;

        public DefaultViewCompiler(
            IList<CompiledViewDescriptor> compiledViews,
            ILogger logger)
        {
            if (compiledViews == null)
            {
                throw new ArgumentNullException(nameof(compiledViews));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
            _normalizedPathCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

            // We need to validate that the all of the precompiled views are unique by path (case-insensitive).
            // We do this because there's no good way to canonicalize paths on windows, and it will create
            // problems when deploying to linux. Rather than deal with these issues, we just don't support
            // views that differ only by case.
            _compiledViews = new Dictionary<string, Task<CompiledViewDescriptor>>(
                compiledViews.Count,
                StringComparer.OrdinalIgnoreCase);

            foreach (var compiledView in compiledViews)
            {
                logger.ViewCompilerLocatedCompiledView(compiledView.RelativePath);

                if (!_compiledViews.ContainsKey(compiledView.RelativePath))
                {
                    // View ordering has precedence semantics, a view with a higher precedence was not
                    // already added to the list.
                    _compiledViews.Add(compiledView.RelativePath, Task.FromResult(compiledView));
                }
            }

            if (_compiledViews.Count == 0)
            {
                logger.ViewCompilerNoCompiledViewsFound();
            }
        }

        /// <inheritdoc />
        public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
            // normalized and a cache entry exists.
            if (_compiledViews.TryGetValue(relativePath, out var cachedResult))
            {
                //_logger.ViewCompilerLocatedCompiledViewForPath(relativePath);
                return cachedResult;
            }

            var normalizedPath = GetNormalizedPath(relativePath);
            if (_compiledViews.TryGetValue(normalizedPath, out cachedResult))
            {
                //_logger.ViewCompilerLocatedCompiledViewForPath(normalizedPath);
                return cachedResult;
            }

            // Entry does not exist. Attempt to create one.
            //_logger.ViewCompilerCouldNotFindFileAtPath(relativePath);
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
