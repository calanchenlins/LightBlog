using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;


namespace KaneBlake.AspNetCore.Extensions.MVC.Module
{
    public static class ApplicationPartManagerExtension
    {
        public static ApplicationPartManager AddModularApplicationPart(this IServiceCollection services, string moduleDir)
        {
            var env = GetServiceFromCollection<IWebHostEnvironment>(services);
            var moduleFileProvider = new PhysicalFileProvider(Path.Combine(env.ContentRootPath, moduleDir));

            services.AddTransient<IConfigureOptions<ModuleMvcOptions>, ModuleMvcOptionsSetup>();
            services.PostConfigure<ModuleMvcOptions>(options =>
            {
                options.FileProviders.Clear();
                options.FileProviders.Add(moduleFileProvider);
            });
            services.AddSingleton<ModuleMvcFileProvider>();
            services.AddSingleton<IModuleChangeProvider, CompositeModuleChangeProvider>();
            services.AddSingleton<IActionDescriptorChangeProvider, ModuleActionDescriptorChangeProvider>();


            var compilerProvider = services.FirstOrDefault(f =>
            f.ServiceType == typeof(IViewCompilerProvider) &&
            f.ImplementationType?.Assembly == typeof(IViewCompilerProvider).Assembly &&
            f.ImplementationType.FullName == "Microsoft.AspNetCore.Mvc.Razor.Compilation.DefaultViewCompilerProvider");

            if (compilerProvider != null)
            {
                // Replace the default implementation of IViewCompilerProvider
                services.Remove(compilerProvider);
            }
            services.TryAddSingleton<IViewCompilerProvider, ModuleViewCompilerProvider>();


            ApplicationPartManager applicationPartManager = GetServiceFromCollection<ApplicationPartManager>(services);

            if (applicationPartManager == null)
            {
                throw new ArgumentNullException(nameof(applicationPartManager));
            }

            applicationPartManager.PopulateModuleParts(GetControllerModules(moduleFileProvider));

            // DI of the module project can't unload
            //var moduleAssemblyParts = applicationPartManager.ApplicationParts.OfType<ControllerModulesAssemblyPart>().ToList();
            //foreach (var moduleAssemblyPart in moduleAssemblyParts)
            //{
            //    foreach (var attribute in moduleAssemblyPart.Assembly.GetCustomAttributes<HostingServiceStartupAttribute>())
            //    {
            //        var hostingStartup = (IHostingServiceStartup)Activator.CreateInstance(attribute.HostingStartupType);
            //        hostingStartup.Configure(services);
            //    }
            //}

            return applicationPartManager;
        }

        private static IEnumerable<string> GetControllerModules(IFileProvider moduleFileProvider)
        {
            var LoadedAssemblies = new List<Assembly>();

            AppDomain.CurrentDomain.GetAssemblies().ToList()
                .ForEach(assembly =>
                {
                    LoadedAssemblies.Add(assembly);
                    LoadedAssemblies.AddRange(RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false));
                });

            var LoadedAssemblyNames = LoadedAssemblies.Select(assembly => assembly.GetName().Name);

            var QueuedAssemblies = moduleFileProvider
                .GetDirectoryContents("/")
                .Where(file => !file.IsDirectory && file.Name.EndsWith(".dll") && !file.Name.EndsWith(".Views.dll"))
                .Where(file => !LoadedAssemblyNames.Contains(file.Name[0..^4]))
                .Select(file => file.PhysicalPath).ToList();

            return QueuedAssemblies;
        }

        private static T GetServiceFromCollection<T>(IServiceCollection services)
        {
            ServiceDescriptor serviceDescriptor = services.LastOrDefault((ServiceDescriptor d) => d.ServiceType == typeof(T));
            return (T)serviceDescriptor?.ImplementationInstance;
        }

        private static void PopulateDefaultParts(this ApplicationPartManager applicationPartManager, string entryAssemblyName)
        {
            applicationPartManager.ApplicationParts.Clear();
            var assemblies = GetApplicationPartAssemblies(entryAssemblyName);

            var seenAssemblies = new HashSet<Assembly>();

            foreach (var assembly in assemblies)
            {
                if (!seenAssemblies.Add(assembly))
                {
                    // "assemblies" may contain duplicate values, but we want unique ApplicationPart instances.
                    // Note that we prefer using a HashSet over Distinct since the latter isn't
                    // guaranteed to preserve the original ordering.
                    continue;
                }

                var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
                foreach (var applicationPart in partFactory.GetApplicationParts(assembly))
                {
                    applicationPartManager.ApplicationParts.Add(applicationPart);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void PopulateModuleParts(this ApplicationPartManager applicationPartManager, IEnumerable<string> modulesPath)
        {
            foreach (var assemblyPath in modulesPath)
            {
                var fileName = Path.GetFileName(assemblyPath) ?? string.Empty;
                if (fileName.EndsWith(".dll"))
                {
                    fileName = fileName[0..^4];
                }
                if (applicationPartManager.ApplicationParts.Any(part => part.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                var loadContext = new ModuleAssemblyLoadContext(assemblyPath);

                if (ModuleInfo.ModuleContexts.TryAdd(assemblyPath + Guid.NewGuid(), new WeakReference<ModuleAssemblyLoadContext>(loadContext, true)))
                {
                    var ApplicationParts = loadContext.LoadModuleAssemblyParts(true);
                    foreach (var razorAssemblyPart in ApplicationParts)
                    {
                        applicationPartManager.ApplicationParts.Add(razorAssemblyPart);
                    }
                }
                else
                {
                    loadContext.Dispose();
                }
            }
        }

        private static IEnumerable<Assembly> GetApplicationPartAssemblies(string entryAssemblyName)
        {
            var assemblyLoadContext = AssemblyLoadContext.Default;
            var entryAssembly = Assembly.Load(new AssemblyName(entryAssemblyName));

            // Use ApplicationPartAttribute to get the closure of direct or transitive dependencies
            // that reference MVC.
            var assembliesFromAttributes = entryAssembly.GetCustomAttributes<ApplicationPartAttribute>()
                .Select(name => Assembly.Load(name.AssemblyName))
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
                .SelectMany(GetAssemblyClosure);

            // The SDK will not include the entry assembly as an application part. We'll explicitly list it
            // and have it appear before all other assemblies \ ApplicationParts.
            return GetAssemblyClosure(entryAssembly)
                .Concat(assembliesFromAttributes);
        }

        private static IEnumerable<Assembly> GetAssemblyClosure(Assembly assembly)
        {
            yield return assembly;

            var relatedAssemblies = RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false)
                .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal);

            foreach (var relatedAssembly in relatedAssemblies)
            {
                yield return relatedAssembly;
            }
        }
    }
}
