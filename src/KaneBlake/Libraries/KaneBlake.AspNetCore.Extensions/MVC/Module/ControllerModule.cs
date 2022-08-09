using K.AspNetCore.Extensions.MVC.Module.ApplicationParts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Threading;

namespace K.AspNetCore.Extensions.MVC.Module
{
    /// <summary>
    /// Implements <see cref="IChangeToken"/>
    /// </summary>
    public class ConfigurationReloadToken : IChangeToken
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Indicates if this token will proactively raise callbacks. Callbacks are still guaranteed to be invoked, eventually.
        /// </summary>
        /// <returns>True if the token will proactively raise callbacks.</returns>
        public bool ActiveChangeCallbacks => true;

        /// <summary>
        /// Gets a value that indicates if a change has occurred.
        /// </summary>
        /// <returns>True if a change has occurred.</returns>
        public bool HasChanged => _cts.IsCancellationRequested;

        /// <summary>
        /// Registers for a callback that will be invoked when the entry has changed. <see cref="Microsoft.Extensions.Primitives.IChangeToken.HasChanged"/>
        /// MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="state">State to be passed into the callback.</param>
        /// <returns>The <see cref="CancellationToken"/> registration.</returns>
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => _cts.Token.Register(callback, state);

        /// <summary>
        /// Used to trigger the change token when a reload occurs.
        /// </summary>
        public void OnReload() => _cts.Cancel();
    }

    public class ModuleMvcOptions
    {
        /// <summary>
        /// Gets the <see cref="IFileProvider" /> instances used to locate Razor files.
        /// </summary>
        /// <remarks>
        /// At startup, this collection is initialized to include an instance of
        /// <see cref="IHostingEnvironment.ContentRootFileProvider"/> that is rooted at the application root.
        /// </remarks>
        public IList<IFileProvider> FileProviders { get; } = new List<IFileProvider>();
    }

    public class ModuleMvcOptionsSetup : IConfigureOptions<ModuleMvcOptions>
    {
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ModuleMvcOptionsSetup(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        public void Configure(ModuleMvcOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
        }
    }

    public class ModuleMvcFileProvider
    {
        private readonly ModuleMvcOptions _options;
        private IFileProvider _compositeFileProvider;

        public ModuleMvcFileProvider(IOptions<ModuleMvcOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        public IFileProvider FileProvider
        {
            get
            {
                if (_compositeFileProvider == null)
                {
                    _compositeFileProvider = GetCompositeFileProvider(_options);
                }

                return _compositeFileProvider;
            }
        }

        private static IFileProvider GetCompositeFileProvider(ModuleMvcOptions options)
        {
            var fileProviders = options.FileProviders;
            if (fileProviders.Count == 0)
            {
                throw new InvalidOperationException("FileProviders Are Required:" + nameof(ModuleMvcOptions.FileProviders));
            }
            else if (fileProviders.Count == 1)
            {
                return fileProviders[0];
            }

            return new CompositeFileProvider(fileProviders);
        }
    }

    public class ModuleChangeProvider : IDisposable
    {
        private readonly ApplicationPartManager _partManager;
        private readonly IFileProvider _fileProvider;
        private readonly IList<IDisposable> _changeTokenRegistrations;
        private ConfigurationReloadToken _changeToken = new ConfigurationReloadToken();

        public ModuleChangeProvider(ApplicationPartManager partManager, IFileProvider fileProvider, string entryAssemblyPath, IEnumerable<string> assemblyPaths)
        {
            _partManager = partManager ?? throw new ArgumentNullException(nameof(partManager));
            _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));

            _changeTokenRegistrations = new List<IDisposable>(assemblyPaths.Count());

            foreach (var filePath in assemblyPaths)
            {
                var fileName = Path.GetFileName(filePath);
                _changeTokenRegistrations.Add(
                    ChangeToken.OnChange(
                        () => _fileProvider.Watch("/" + fileName),
                        OnModuleChanged,
                        entryAssemblyPath));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnModuleChanged(string entryAssemblyPath)
        {
            var loadContextName = nameof(ModuleAssemblyLoadContext) + ":" + entryAssemblyPath;

            var loadContexts = AssemblyLoadContext.All.Where(context => loadContextName.Equals(context.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            var moduleAssemblyParts = _partManager.ApplicationParts.OfType<ModulesAssemblyPart>().Where(part => part.EntryAssemblyPath.Equals(entryAssemblyPath)).ToList();

            var newLoadContext = new ModuleAssemblyLoadContext(entryAssemblyPath);

            IList<ModulesAssemblyPart> ApplicationParts = null;

            if (ModuleInfo.ModuleContexts.TryAdd(entryAssemblyPath + Guid.NewGuid(), new WeakReference<ModuleAssemblyLoadContext>(newLoadContext, true)))
            {
                try
                {
                    ApplicationParts = newLoadContext.LoadModuleAssemblyParts(true);
                }
                catch
                {
                    newLoadContext.Dispose();
                }
            }
            else
            {
                newLoadContext.Dispose();
            }

            if (ApplicationParts != null)
            {
                moduleAssemblyParts.ForEach(part =>
                {
                    part.Dispose();
                    _partManager.ApplicationParts.Remove(part);
                });
                foreach (var loadContext in loadContexts)
                {
                    if (loadContext is ModuleAssemblyLoadContext moduleLoadContext)
                    {
                        moduleLoadContext.Dispose();
                    }
                }
                foreach (var part in ApplicationParts)
                {
                    _partManager.ApplicationParts.Add(part);
                }
                RaiseChanged();
            }
        }
        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        public IChangeToken GetChangeToken() => _changeToken;

        public void Dispose()
        {
            // dispose change token registrations
            foreach (var registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }
        }
    }

    public interface IModuleChangeProvider
    {
        IChangeToken GetChangeToken(string entryAssemblyPath);
        IEnumerable<string> EntryAssemblyPaths { get; }
    }

    public class CompositeModuleChangeProvider : IModuleChangeProvider
    {
        private readonly ConcurrentDictionary<string, ModuleChangeProvider> _moduleChangeTokens;

        public CompositeModuleChangeProvider(ApplicationPartManager partManager, ModuleMvcFileProvider moduleFileProvider)
        {
            var moduleAssemblyParts = partManager.ApplicationParts.OfType<ModulesAssemblyPart>().ToList();

            var _ = moduleAssemblyParts.GroupBy(r => r.EntryAssemblyPath).Select(g => new { entryAssemblyPath = g.Key, assemblyPaths = g.Select(r => r.AssemblyPath) }).ToList();
            _moduleChangeTokens = new ConcurrentDictionary<string, ModuleChangeProvider>();

            foreach (var __ in _)
            {
                var moduleChangeToken = new ModuleChangeProvider(partManager, moduleFileProvider.FileProvider, __.entryAssemblyPath, __.assemblyPaths);
                _moduleChangeTokens.TryAdd(__.entryAssemblyPath, moduleChangeToken);
            }
        }

        public IEnumerable<string> EntryAssemblyPaths => _moduleChangeTokens.Keys;

        public IChangeToken GetChangeToken(string entryAssemblyPath)
        {
            _moduleChangeTokens.TryGetValue(entryAssemblyPath, out var changeToken);
            return changeToken.GetChangeToken();
        }
    }

    // one controller'DLL effect all controllers'DLL
    public class ModuleActionDescriptorChangeProvider : IActionDescriptorChangeProvider, IDisposable
    {
        private readonly IList<IDisposable> _changeTokenRegistrations;

        private ConfigurationReloadToken _changeToken = new ConfigurationReloadToken();

        private readonly IModuleChangeProvider _moduleChangeProvider;

        public ModuleActionDescriptorChangeProvider(
            IModuleChangeProvider moduleChangeProvider)
        {
            _moduleChangeProvider = moduleChangeProvider ?? throw new ArgumentNullException(nameof(moduleChangeProvider));

            _changeTokenRegistrations = new List<IDisposable>(_moduleChangeProvider.EntryAssemblyPaths.Count());

            foreach (var entryAssemblyPath in _moduleChangeProvider.EntryAssemblyPaths)
            {
                _changeTokenRegistrations.Add(
                    ChangeToken.OnChange(
                        () => _moduleChangeProvider.GetChangeToken(entryAssemblyPath),
                        RaiseChanged));
            }

        }

        private void RaiseChanged()
        {
            var previousToken = Interlocked.Exchange(ref _changeToken, new ConfigurationReloadToken());
            previousToken.OnReload();
        }

        public IChangeToken GetChangeToken() => _changeToken;

        /// <inheritdoc />
        public void Dispose()
        {
            // dispose change token registrations
            foreach (var registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }
        }

    }
}
