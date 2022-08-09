using System;
using System.Collections.Generic;
using System.Text;

namespace K.AspNetCore.Extensions.MVC.Module
{
    public static class ModuleInfo
    {
        /// <summary>
        /// for check custom AssemblyLoadContext's state
        /// </summary>
        public static Dictionary<string, WeakReference<ModuleAssemblyLoadContext>> ModuleContexts { get; }
        static ModuleInfo()
        {
            ModuleContexts = new Dictionary<string, WeakReference<ModuleAssemblyLoadContext>>();
        }
    }
}
