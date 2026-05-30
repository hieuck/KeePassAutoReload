namespace KeePassAutoReload
{
    internal static class PluginAboutInfo
    {
        public static string BuildText(string version, int intervalSeconds, bool skipModified)
        {
            string skipModifiedText = skipModified ? "Yes" : "No";

            return "KeePass Auto Reload\r\n" +
                "Version: " + version + "\r\n" +
                "Interval: " + intervalSeconds + " seconds\r\n" +
                "Skip modified databases: " + skipModifiedText + "\r\n\r\n" +
                "Automatically synchronizes the active KeePass database with its current source.";
        }
    }
}
