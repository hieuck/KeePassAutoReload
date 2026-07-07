using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
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
        UpdateCheckerHandlesSemanticVersionMetadata();
        UpdateCheckerSelectsNewestSemanticTag();
        UpdateCheckerFetchesLatestReleaseFromInjectedClientAsync().Wait();
        UpdateCheckerDoesNotMutateGlobalSecurityProtocol();
        HttpUpdateClientUsesModernTls();
        HttpUpdateClientHasReasonableDefaultTimeout();
        PluginPathResolverUsesAssemblyLocationWhenAvailable();
        PluginPathResolverFallsBackToPluginsDirectory();
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

    private static void UpdateCheckerHandlesSemanticVersionMetadata()
    {
        AssertTrue(UpdateChecker.IsNewerVersion("1.0.0+c83c367", "v1.0.1"), "semver metadata in current version should be ignored");
        AssertFalse(UpdateChecker.IsNewerVersion("1.0.1+abc123", "1.0.1"), "same version with different metadata should not update");
    }

    private static void UpdateCheckerSelectsNewestSemanticTag()
    {
        string tag = UpdateChecker.GetNewestVersionTag(new string[] { "latest", "v1.0.1", "v1.1.0", "draft" });
        AssertEqual("v1.1.0", tag, "newest semantic release tag should be selected");
    }

    private static async Task UpdateCheckerFetchesLatestReleaseFromInjectedClientAsync()
    {
        string json = "[{\"tag_name\":\"v1.0.1\"},{\"tag_name\":\"v1.1.0\"}]";
        FakeUpdateClient client = new FakeUpdateClient { Response = json };
        UpdateInfo info = await UpdateChecker.CheckLatestAsync(client);
        AssertEqual("v1.1.0", info.LatestVersion, "latest version should be parsed from injected client response");
        AssertTrue(info.IsUpdateAvailable, "an update should be available when injected response has newer version");
    }

    private static void UpdateCheckerDoesNotMutateGlobalSecurityProtocol()
    {
        SecurityProtocolType before = ServicePointManager.SecurityProtocol;
        UpdateChecker.CheckLatestAsync(new FakeUpdateClient { Response = "[{\"tag_name\":\"v1.0.1\"}]" }).Wait();
        SecurityProtocolType after = ServicePointManager.SecurityProtocol;
        AssertEqual(before, after, "update check should not mutate global ServicePointManager.SecurityProtocol");
    }

    private static void HttpUpdateClientUsesModernTls()
    {
        using (HttpUpdateClient client = new HttpUpdateClient())
        {
            AssertTrue(client.Handler.SslProtocols.HasFlag(SslProtocols.Tls12) || client.Handler.SslProtocols.HasFlag(SslProtocols.Tls13),
                "HttpUpdateClient should enable TLS 1.2 or higher");
        }
    }

    private static void HttpUpdateClientHasReasonableDefaultTimeout()
    {
        using (HttpUpdateClient client = new HttpUpdateClient())
        {
            AssertTrue(client.Timeout.TotalSeconds > 0 && client.Timeout.TotalSeconds <= 120,
                "HttpUpdateClient should have a reasonable default timeout (0 < timeout <= 120s)");
        }
    }

    private static void PluginPathResolverUsesAssemblyLocationWhenAvailable()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            string result = PluginPathResolver.ResolvePluginPackagePath(tempFile, Path.GetTempPath());
            AssertEqual(tempFile, result, "resolver should return the existing assembly location");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static void PluginPathResolverFallsBackToPluginsDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            string result = PluginPathResolver.ResolvePluginPackagePath(null, tempDir);
            string expected = Path.Combine(tempDir, "Plugins", "KeePassAutoReload.dll");
            AssertEqual(expected, result, "resolver should fall back to KeePass Plugins directory");
            AssertTrue(Directory.Exists(Path.Combine(tempDir, "Plugins")), "fallback should create Plugins directory");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
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

    private sealed class FakeUpdateClient : IUpdateClient
    {
        public string Response { get; set; }
        public byte[] FileData { get; set; }
        public string LastDownloadDestination { get; private set; }

        public Task<string> DownloadStringAsync(string url)
        {
            return Task.FromResult(Response ?? string.Empty);
        }

        public Task DownloadFileAsync(string url, string destinationPath)
        {
            LastDownloadDestination = destinationPath;
            if (FileData != null)
            {
                File.WriteAllBytes(destinationPath, FileData);
            }
            return Task.CompletedTask;
        }
    }
}
