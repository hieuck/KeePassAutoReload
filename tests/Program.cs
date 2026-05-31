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
        UpdateCheckerDetectsNewerSemanticVersions();
        UpdateCheckerIgnoresSameOrInvalidVersions();
        UpdateCheckerSelectsNewestSemanticTag();
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

    private static void UpdateCheckerDetectsNewerSemanticVersions()
    {
        AssertTrue(UpdateChecker.IsNewerVersion("1.0.1", "v1.0.2"), "patch update should be detected");
        AssertTrue(UpdateChecker.IsNewerVersion("1.0.1", "v1.1.0"), "minor update should be detected");
    }

    private static void UpdateCheckerIgnoresSameOrInvalidVersions()
    {
        AssertFalse(UpdateChecker.IsNewerVersion("1.0.1", "1.0.1"), "same version should not update");
        AssertFalse(UpdateChecker.IsNewerVersion("1.0.1", "latest"), "non-version tag should not update");
    }

    private static void UpdateCheckerSelectsNewestSemanticTag()
    {
        string tag = UpdateChecker.GetNewestVersionTag(new string[] { "latest", "v1.0.1", "v1.1.0", "draft" });
        AssertEqual("v1.1.0", tag, "newest semantic release tag should be selected");
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

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!object.Equals(expected, actual))
        {
            throw new Exception(message + ". Expected: " + expected + ", actual: " + actual);
        }
    }
}
