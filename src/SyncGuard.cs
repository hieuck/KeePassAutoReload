namespace KeePassAutoReload
{
    internal static class SyncGuard
    {
        public static bool CanRunSync(bool hasHost, bool hasDatabase, bool hasMainWindow)
        {
            return hasHost && hasDatabase && hasMainWindow;
        }
    }
}
