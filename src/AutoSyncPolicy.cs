namespace KeePassAutoReload
{
    internal static class AutoSyncPolicy
    {
        public static bool ShouldRun(bool isOpen, bool isModified, bool skipWhenModified)
        {
            if (!isOpen) return false;
            if (isModified && skipWhenModified) return false;

            return true;
        }
    }
}
