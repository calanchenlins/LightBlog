using KaneBlake.AspNetCore.Extensions.MVC.Module.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.MVC.Module
{
    public class ModuleAssemblyLoadContext : AssemblyLoadContext, IDisposable
    {
        public string ControllerModulePath { get; private set; }

        public string ControllerModuleDir { get; private set; }
        public ModuleAssemblyLoadContext(string entryAssemblyPath) : base(nameof(ModuleAssemblyLoadContext) + ":" + entryAssemblyPath, isCollectible: true)
        {
            ControllerModulePath = entryAssemblyPath;
            ControllerModuleDir = Path.GetDirectoryName(ControllerModulePath);
            // First, load referenced assembly from AssemblyLoadContext.Default.
            // If failed, we should handle it manually
            Resolving += (AssemblyLoadContext, AssemblyName) =>
            {
                var referenceAssemblyPath = Path.Combine(Path.GetDirectoryName(ControllerModulePath), AssemblyName.Name + ".dll");
                if (!File.Exists(referenceAssemblyPath))
                {
                    return null;
                }
                using var fs = new FileStream(referenceAssemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                return AssemblyLoadContext.LoadFromStream(fs);
            };
        }

        public IList<ModulesAssemblyPart> LoadModuleAssemblyParts(bool includeRazorView = false)
        {
            if (string.IsNullOrEmpty(ControllerModulePath) || !File.Exists(ControllerModulePath))
            {
                return Array.Empty<ModulesAssemblyPart>(); ;
            }
            var ApplicationParts = new List<ModulesAssemblyPart>();
            using var fs = new FileStream(ControllerModulePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var controllerAssembly = LoadFromStream(fs);
            var controllerPart = new ControllerModulesAssemblyPart(ControllerModulePath, controllerAssembly);
            ApplicationParts.Add(controllerPart);

            // todo:we should load referenced DLLs, otherwise module's referenced DLLs can't Implementing hotUpdate 
            var referencedAssemblyNames = controllerAssembly.GetReferencedAssemblies();
            foreach (var referencedAssemblyName in referencedAssemblyNames)
            {
                var referencedAssemblyLocation = Path.Combine(ControllerModuleDir, referencedAssemblyName.Name + ".dll");
                if (!File.Exists(referencedAssemblyLocation))
                {
                    continue;
                }
                using var referenceFs = new FileStream(referencedAssemblyLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

                var referencedAssembly = LoadFromStream(referenceFs);
                var referencedPart = new ModulesAssemblyPart(ControllerModulePath, referencedAssemblyLocation, referencedAssembly);
                ApplicationParts.Add(referencedPart);

            }

            if (includeRazorView)
            {
                var parts = LoadRazorAssemblyPart(controllerPart);
                parts.ToList().ForEach(part => ApplicationParts.Add(part));
            }
            return ApplicationParts;
        }

        private IEnumerable<ModulesAssemblyPart> LoadRazorAssemblyPart(ControllerModulesAssemblyPart assemblyPart)
        {
            if (assemblyPart == null)
            {
                throw new ArgumentNullException(nameof(assemblyPart));
            }

            var assembly = assemblyPart.Assembly;

            var attributes = assembly.GetCustomAttributes<RelatedAssemblyAttribute>().ToArray();
            if (attributes.Length == 0)
            {
                return Array.Empty<ModulesAssemblyPart>();
            }

            var assemblyName = assembly.GetName().Name;

            var relatedAssemblies = new List<CompiledRazorModulesAssemblyPart>(attributes.Length);
            for (var i = 0; i < attributes.Length; i++)
            {
                var attribute = attributes[i];
                if (string.Equals(assemblyName, attribute.AssemblyFileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"{assemblyName}:Assembly Cannot Reference Self In RelatedAssemblyAttribute.");
                }

                var relatedAssemblyLocation = Path.Combine(ControllerModuleDir, attribute.AssemblyFileName + ".dll");
                if (!File.Exists(relatedAssemblyLocation))
                {
                    continue;
                }
                using var fs = new FileStream(relatedAssemblyLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var viewAssembly = LoadFromStream(fs);
                var viewAssemblyPart = new CompiledRazorModulesAssemblyPart(assemblyPart.EntryAssemblyPath, relatedAssemblyLocation, viewAssembly);
                relatedAssemblies.Add(viewAssemblyPart);
            }

            return relatedAssemblies;
        }

        protected override Assembly Load(AssemblyName name)
        {
            return null;
        }

        private bool disposedValue = false;
        public void Dispose()
        {
            if (!disposedValue)
            {
                Unload();
                disposedValue = true;
            }
        }
    }
}
