using System;
using KeePassAutoReload;

internal static class Program
{
    private static int Main()
    {
        RequiresOpenDatabase();
        SkipsModifiedDatabaseWhenConfigured();
        AllowsModifiedDatabaseWhenConfigured();
        return 0;
    }

    private static void RequiresOpenDatabase()
    {
        AssertFalse(AutoSyncPolicy.ShouldRun(false, false, true), "closed database must not sync");
    }

    private static void SkipsModifiedDatabaseWhenConfigured()
    {
        AssertFalse(AutoSyncPolicy.ShouldRun(true, true, true), "modified database should be skipped by default");
    }

    private static void AllowsModifiedDatabaseWhenConfigured()
    {
        AssertTrue(AutoSyncPolicy.ShouldRun(true, true, false), "manual sync can include modified database");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void AssertFalse(bool value, string message)
    {
        if (value) throw new Exception(message);
    }
}
