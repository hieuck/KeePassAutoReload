using System;
using KeePassAutoReload;

internal static class Program
{
    private static int Main()
    {
        RequiresOpenDatabase();
        SkipsModifiedDatabaseWhenConfigured();
        AllowsModifiedDatabaseWhenConfigured();
        AboutTextIncludesVersionAndCurrentSettings();
        DetectsNewerUpdateVersion();
        RejectsSameOrOlderUpdateVersion();
        UpdateMessageIncludesRestartInstruction();
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

    private static void AboutTextIncludesVersionAndCurrentSettings()
    {
        string text = PluginAboutInfo.BuildText("1.2.3.4", 30, true);

        AssertContains(text, "KeePass Auto Reload");
        AssertContains(text, "Version: 1.2.3.4");
        AssertContains(text, "Interval: 30 seconds");
        AssertContains(text, "Skip modified databases: Yes");
    }

    private static void DetectsNewerUpdateVersion()
    {
        AssertTrue(PluginUpdateInfo.IsRemoteNewer("1.0.1.0", "1.0.2.0"), "newer remote version should update");
    }

    private static void RejectsSameOrOlderUpdateVersion()
    {
        AssertFalse(PluginUpdateInfo.IsRemoteNewer("1.0.1.0", "1.0.1.0"), "same remote version should not update");
        AssertFalse(PluginUpdateInfo.IsRemoteNewer("1.0.1.0", "1.0.0.0"), "older remote version should not update");
    }

    private static void UpdateMessageIncludesRestartInstruction()
    {
        string text = PluginUpdateInfo.BuildStagedUpdateMessage("1.0.1.0", "1.0.2.0");

        AssertContains(text, "Current version: 1.0.1.0");
        AssertContains(text, "New version: 1.0.2.0");
        AssertContains(text, "Close KeePass");
    }

    private static void AssertTrue(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void AssertFalse(bool value, string message)
    {
        if (value) throw new Exception(message);
    }

    private static void AssertContains(string value, string expected)
    {
        if (value == null || !value.Contains(expected))
        {
            throw new Exception("expected text to contain: " + expected);
        }
    }
}
