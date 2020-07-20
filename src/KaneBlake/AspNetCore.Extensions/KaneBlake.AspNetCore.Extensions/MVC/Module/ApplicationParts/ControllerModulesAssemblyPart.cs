using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace KaneBlake.AspNetCore.Extensions.MVC.Module.ApplicationParts
{
    /// <summary>
    /// An <see cref="ControllerModulesAssemblyPart"/> for Controller assemblies.
    /// </summary>
    public class ControllerModulesAssemblyPart : ModulesAssemblyPart, IApplicationPartTypeProvider
    {
        /// <summary>
        /// Initializes a new <see cref="ControllerModuleAssemblyPart"/> instance.
        /// </summary>
        /// <param name="assemblyPath">FilePath of Assembly</param>
        /// <param name="loadContext">The backing <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.</param>
        public ControllerModulesAssemblyPart(string assemblyPath, Assembly assemblyFromStream) : base(entryAssemblyPath: assemblyPath, assemblyPath, assemblyFromStream)
        {
        }

        /// <inheritdoc />
        public IEnumerable<TypeInfo> Types => Assembly.DefinedTypes;
    }
}
