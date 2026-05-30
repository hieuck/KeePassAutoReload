using System;

namespace KeePassAutoReload
{
    internal static class PluginUpdateInfo
    {
        public const string LatestDllUrl = "https://github.com/hieuck/KeePassAutoReload/releases/latest/download/KeePassAutoReload.dll";

        public static bool IsRemoteNewer(string currentVersion, string remoteVersion)
        {
            Version current = ParseVersion(currentVersion);
            Version remote = ParseVersion(remoteVersion);

            return remote.CompareTo(current) > 0;
        }

        public static string BuildStagedUpdateMessage(string currentVersion, string remoteVersion)
        {
            return "A newer KeePass Auto Reload update has been staged.\r\n\r\n" +
                "Current version: " + currentVersion + "\r\n" +
                "New version: " + remoteVersion + "\r\n\r\n" +
                "Close KeePass to install it. The updater will replace the plugin DLL after KeePass exits.";
        }

        private static Version ParseVersion(string value)
        {
            Version version;
            if (Version.TryParse(value, out version)) return version;

            return new Version(0, 0, 0, 0);
        }
    }
}
