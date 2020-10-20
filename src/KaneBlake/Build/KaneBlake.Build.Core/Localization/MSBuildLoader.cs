using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace KaneBlake.Build.Core.Localization
{
    public static class MSBuildLoader
    {
        public static bool TryLoad(out string msBuildPath)
        {
            try
            {
                var vsInstance = MSBuildLocator.RegisterDefaults();
                msBuildPath = vsInstance.MSBuildPath;

                // Since we do not inherit msbuild.deps.json when referencing the SDK copy
                // of MSBuild and because the SDK no longer ships with version matched assemblies, we
                // register an assembly loader that will load assemblies from the msbuild path with
                // equal or higher version numbers than requested.
                LooseVersionAssemblyLoader.Register(msBuildPath);

                return true;
            }
            catch
            {
                msBuildPath = null;
                return false;
            }
        }
        internal static class LooseVersionAssemblyLoader
        {
            private static readonly Dictionary<string, Assembly> s_pathsToAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            private static readonly Dictionary<string, Assembly> s_namesToAssemblies = new Dictionary<string, Assembly>();

            private static readonly object s_guard = new object();

            /// <summary>
            /// Register an assembly loader that will load assemblies with higher version than what was requested.
            /// </summary>
            public static void Register(string searchPath)
            {
                AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
                {
                    lock (s_guard)
                    {
                        if (s_namesToAssemblies.TryGetValue(assemblyName.FullName, out var cachedAssembly))
                        {
                            return cachedAssembly;
                        }

                        var assembly = AssemblyResolver.TryResolveAssemblyFromPaths(context, assemblyName, searchPath, s_pathsToAssemblies);

                        // Cache assembly
                        if (assembly != null)
                        {
                            var name = assembly.FullName;
                            if (name is null)
                            {
                                throw new Exception($"Could not get name for assembly '{assembly}'");
                            }

                            s_pathsToAssemblies[assembly.Location] = assembly;
                            s_namesToAssemblies[name] = assembly;
                        }

                        return assembly;
                    }
                };
            }
        }


        internal static class AssemblyResolver
        {
            private static readonly string[] s_extensions = new[] { "ni.dll", "ni.exe", "dll", "exe" };

            internal static Assembly? TryResolveAssemblyFromPaths(AssemblyLoadContext context, AssemblyName assemblyName, string searchPath, Dictionary<string, Assembly>? knownAssemblyPaths = null)
            {
                foreach (var cultureSubfolder in string.IsNullOrEmpty(assemblyName.CultureName)
                    // If no culture is specified, attempt to load directly from
                    // the known dependency paths.
                    ? new[] { string.Empty }
                    // Search for satellite assemblies in culture subdirectories
                    // of the assembly search directories, but fall back to the
                    // bare search directory if that fails.
                    : new[] { assemblyName.CultureName, string.Empty })
                {
                    foreach (var extension in s_extensions)
                    {
                        var candidatePath = Path.Combine(
                            searchPath, cultureSubfolder, $"{assemblyName.Name}.{extension}");

                        var isAssemblyLoaded = knownAssemblyPaths?.ContainsKey(candidatePath) == true;
                        if (isAssemblyLoaded || !File.Exists(candidatePath))
                        {
                            continue;
                        }

                        var candidateAssemblyName = AssemblyLoadContext.GetAssemblyName(candidatePath);
                        if (candidateAssemblyName.Version < assemblyName.Version)
                        {
                            continue;
                        }

                        try
                        {
                            return context.LoadFromAssemblyPath(candidatePath);
                        }
                        catch
                        {
                            if (assemblyName.Name != null)
                            {
                                // We were unable to load the assembly from the file path. It is likely that
                                // a different version of the assembly has already been loaded into the context.
                                // Be forgiving and attempt to load assembly by name without specifying a version.
                                return context.LoadFromAssemblyName(new AssemblyName(assemblyName.Name));
                            }
                        }
                    }
                }

                return null;
            }
        }
    }
}
