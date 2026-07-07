using System;
using System.IO;

namespace KeePassAutoReload
{
    internal static class PluginPathResolver
    {
        public static string ResolvePluginPackagePath(string assemblyLocation, string keepassExecutableDirectory)
        {
            if (!string.IsNullOrEmpty(assemblyLocation) && File.Exists(assemblyLocation))
            {
                return assemblyLocation;
            }

            if (string.IsNullOrWhiteSpace(keepassExecutableDirectory))
            {
                throw new ArgumentException("KeePass executable directory must not be null, empty, or whitespace.", "keepassExecutableDirectory");
            }

            string pluginsDir = Path.Combine(keepassExecutableDirectory, "Plugins");
            Directory.CreateDirectory(pluginsDir);
            return Path.Combine(pluginsDir, "KeePassAutoReload.dll");
        }
    }
}
