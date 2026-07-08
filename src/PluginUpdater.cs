using System;
using System.Diagnostics;
using System.IO;

namespace KeePassAutoReload
{
    internal interface IProcessStarter
    {
        void Start(string fileName, string arguments);
    }

    internal sealed class ProcessStarter : IProcessStarter
    {
        public void Start(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(startInfo);
        }
    }

    internal static class PluginUpdater
    {
        public static bool TryScheduleUpdate(
            string pluginPath,
            string newPluginPath,
            string updaterExePath,
            int keepassProcessId,
            string keepassExecutablePath,
            IProcessStarter starter)
        {
            if (string.IsNullOrWhiteSpace(pluginPath)) throw new ArgumentException("pluginPath");
            if (string.IsNullOrWhiteSpace(newPluginPath)) throw new ArgumentException("newPluginPath");
            if (string.IsNullOrWhiteSpace(updaterExePath)) throw new ArgumentException("updaterExePath");
            if (starter == null) throw new ArgumentNullException("starter");
            if (keepassProcessId < 0) throw new ArgumentOutOfRangeException("keepassProcessId");

            if (!pluginPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("pluginPath must end with .dll", "pluginPath");
            if (!newPluginPath.EndsWith(".new", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("newPluginPath must end with .new", "newPluginPath");
            if (!updaterExePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("updaterExePath must end with .exe", "updaterExePath");
            if (!string.IsNullOrWhiteSpace(keepassExecutablePath) && !keepassExecutablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("keepassExecutablePath must end with .exe", "keepassExecutablePath");

            if (!File.Exists(newPluginPath)) return false;
            if (!File.Exists(updaterExePath)) return false;

            string arguments = BuildArguments(pluginPath, newPluginPath, keepassProcessId, keepassExecutablePath);
            starter.Start(updaterExePath, arguments);
            return true;
        }

        public static string BuildArguments(
            string pluginPath,
            string newPluginPath,
            int keepassProcessId,
            string keepassExecutablePath)
        {
            string arguments = string.Format(
                "--process-id {0} --source \"{1}\" --destination \"{2}\"",
                keepassProcessId,
                newPluginPath,
                pluginPath);

            if (!string.IsNullOrWhiteSpace(keepassExecutablePath))
            {
                arguments += string.Format(" --restart \"{0}\"", keepassExecutablePath);
            }

            return arguments;
        }
    }
}
