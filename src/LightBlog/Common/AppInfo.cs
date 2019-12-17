using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LightBlog.Common
{
    public static class AppInfo
    {
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        public static string Version { 
            get {
                if (_cache.TryGetValue(nameof(Version), out var _version))
                {
                    return _version;
                }
                _version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                _cache.TryAdd(nameof(Version), _version);
                return _version;
            } 
        }
    }
}
