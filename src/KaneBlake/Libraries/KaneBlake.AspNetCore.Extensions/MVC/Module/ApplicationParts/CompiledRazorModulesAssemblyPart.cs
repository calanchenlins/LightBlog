using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Razor.Hosting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.MVC.Module.ApplicationParts
{
    /// <summary>
    /// An <see cref="CompiledRazorModuleAssemblyPart"/> for compiled Razor assemblies.
    /// </summary>
    public class CompiledRazorModulesAssemblyPart : ModulesAssemblyPart, IRazorCompiledItemProvider
    {
        /// <summary>
        /// Initializes a new <see cref="CompiledRazorModuleAssemblyPart"/> instance.
        /// </summary>
        /// <param name="assemblyPath">FilePath of Assembly</param>
        /// <param name="loadContext">The backing <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.</param>
        public CompiledRazorModulesAssemblyPart(string entryAssemblyPath, string assemblyPath, Assembly assemblyFromStream) : base(entryAssemblyPath, assemblyPath, assemblyFromStream)
        {
        }

        IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
        {
            get
            {
                var loader = new RazorCompiledItemLoader();
                return loader.LoadItems(Assembly);
            }
        }
    }
}
