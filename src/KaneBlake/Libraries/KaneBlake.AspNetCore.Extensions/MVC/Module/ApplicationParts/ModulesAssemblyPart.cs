using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace K.AspNetCore.Extensions.MVC.Module.ApplicationParts
{
    /// <summary>
    /// An <see cref="ModuleAssemblyPart"/> backed by an <see cref="System.Reflection.Assembly"/> From Stream.
    /// </summary>
    public class ModulesAssemblyPart : ApplicationPart, IDisposable
    {
        /// <summary>
        /// Initializes a new <see cref="ModuleAssemblyPart"/> instance.
        /// </summary>
        /// <param name="assemblyPath">FilePath of Assembly</param>
        /// <param name="loadContext">The backing <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.</param>
        public ModulesAssemblyPart(string entryAssemblyPath, string assemblyPath, Assembly assemblyFromStream)
        {
            EntryAssemblyPath = entryAssemblyPath;
            AssemblyPath = assemblyPath;
            Assembly = assemblyFromStream;
        }

        /// <summary>
        /// Gets the <see cref="Assembly"/> of the <see cref="ModuleAssemblyPart"/>.
        /// </summary>
        public Assembly Assembly { get; private set; }

        public string EntryAssemblyPath { get; }

        public string AssemblyPath { get; }

        /// <summary>
        /// Gets the name of the <see cref="ModuleAssemblyPart"/>.
        /// </summary>
        public override string Name => Assembly.GetName().Name;

        private bool disposedValue = false;
        public void Dispose()
        {
            if (!disposedValue)
            {
                Assembly = null;
                disposedValue = true;
            }
        }
    }
}
