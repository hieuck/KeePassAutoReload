using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using KeePassAutoReload;
using Xunit;

namespace KeePassAutoReload.Tests
{
    public class SyncGuardTests
    {
        [Fact]
        public void AllowsSyncWhenAllConditionsAreTrue()
        {
            Assert.True(SyncGuard.CanRunSync(true, true, true), "sync should run when host, database, and main window are present");
        }

        [Theory]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        public void BlocksSyncWhenAnyConditionIsFalse(bool hasHost, bool hasDatabase, bool hasMainWindow)
        {
            Assert.False(SyncGuard.CanRunSync(hasHost, hasDatabase, hasMainWindow), "sync should be blocked when any required condition is false");
        }
    }

    public class AutoSyncPolicyTests
    {
        [Fact]
        public void RequiresOpenDatabase()
        {
            Assert.False(AutoSyncPolicy.ShouldRun(false, false, true), "closed database must not sync");
        }

        [Fact]
        public void ClosedDatabaseNeverRunsEvenIfModifiedAllowed()
        {
            Assert.False(AutoSyncPolicy.ShouldRun(false, false, false), "closed database must not sync even when modified is allowed");
        }

        [Fact]
        public void SkipsModifiedDatabaseWhenConfigured()
        {
            Assert.False(AutoSyncPolicy.ShouldRun(true, true, true), "modified database should be skipped by default");
        }

        [Fact]
        public void AllowsModifiedDatabaseWhenConfigured()
        {
            Assert.True(AutoSyncPolicy.ShouldRun(true, true, false), "manual sync can include modified database");
        }

        [Fact]
        public void RunsForOpenUnmodifiedDatabase()
        {
            Assert.True(AutoSyncPolicy.ShouldRun(true, false, true), "open unmodified database should sync");
            Assert.True(AutoSyncPolicy.ShouldRun(true, false, false), "open unmodified database should sync regardless of skip-modified");
        }
    }

    public class PluginAboutInfoTests
    {
        [Fact]
        public void AboutTextIncludesVersionAndCurrentSettings()
        {
            string text = PluginAboutInfo.BuildText("1.2.3.4", 30, true);

            Assert.Contains("KeePass Auto Reload", text);
            Assert.Contains("Version: 1.2.3.4", text);
            Assert.Contains("Interval: 30 seconds", text);
            Assert.Contains("Skip modified databases: Yes", text);
        }
    }

    public class UpdateCheckerTests
    {
        [Fact]
        public void DetectsNewerSemanticVersions()
        {
            Assert.True(UpdateChecker.IsNewerVersion("1.0.1", "v1.0.2"), "patch update should be detected");
            Assert.True(UpdateChecker.IsNewerVersion("1.0.1", "v1.1.0"), "minor update should be detected");
        }

        [Fact]
        public void IgnoresSameOrInvalidVersions()
        {
            Assert.False(UpdateChecker.IsNewerVersion("1.0.1", "1.0.1"), "same version should not update");
            Assert.False(UpdateChecker.IsNewerVersion("1.0.1", "latest"), "non-version tag should not update");
        }

        [Fact]
        public void HandlesSemanticVersionMetadata()
        {
            Assert.True(UpdateChecker.IsNewerVersion("1.0.0+c83c367", "v1.0.1"), "semver metadata in current version should be ignored");
            Assert.False(UpdateChecker.IsNewerVersion("1.0.1+abc123", "1.0.1"), "same version with different metadata should not update");
        }

        [Fact]
        public void SelectsNewestSemanticTag()
        {
            string tag = UpdateChecker.GetNewestVersionTag(new string[] { "latest", "v1.0.1", "v1.1.0", "draft" });
            Assert.Equal("v1.1.0", tag);
        }

        [Fact]
        public async Task FetchesLatestReleaseFromInjectedClient()
        {
            string json = "[{\"tag_name\":\"v1.0.1\"},{\"tag_name\":\"v1.1.0\"}]";
            FakeUpdateClient client = new FakeUpdateClient { Response = json };
            UpdateInfo info = await UpdateChecker.CheckLatestAsync(client);
            Assert.Equal("v1.1.0", info.LatestVersion);
            Assert.True(info.IsUpdateAvailable, "an update should be available when injected response has newer version");
        }

        [Fact]
        public async Task PropagatesCancellationTokenToInjectedClient()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                FakeUpdateClient client = new FakeUpdateClient { Response = "[{\"tag_name\":\"v1.0.1\"}]" };
                await UpdateChecker.CheckLatestAsync(client, cts.Token);
                Assert.True(client.LastCancellationToken.HasValue, "cancellation token should be propagated to client");
                Assert.Equal(cts.Token, client.LastCancellationToken.Value);
            }
        }

        [Fact]
        public async Task ThrowsOperationCanceledExceptionWhenAlreadyCanceled()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();
                FakeUpdateClient client = new FakeUpdateClient { Response = "[{\"tag_name\":\"v1.0.1\"}]" };
                await Assert.ThrowsAsync<OperationCanceledException>(() => UpdateChecker.CheckLatestAsync(client, cts.Token));
            }
        }

        [Fact]
        public async Task DoesNotMutateGlobalSecurityProtocol()
        {
            SecurityProtocolType before = ServicePointManager.SecurityProtocol;
            await UpdateChecker.CheckLatestAsync(new FakeUpdateClient { Response = "[{\"tag_name\":\"v1.0.1\"}]" });
            SecurityProtocolType after = ServicePointManager.SecurityProtocol;
            Assert.Equal(before, after);
        }
    }

    public class HttpUpdateClientTests
    {
        [Fact]
        public void UsesModernTls()
        {
            using (HttpUpdateClient client = new HttpUpdateClient())
            {
                Assert.True(client.Handler.SslProtocols.HasFlag(SslProtocols.Tls12) || client.Handler.SslProtocols.HasFlag(SslProtocols.Tls13),
                    "HttpUpdateClient should enable TLS 1.2 or higher");
            }
        }

        [Fact]
        public void HasReasonableDefaultTimeout()
        {
            using (HttpUpdateClient client = new HttpUpdateClient())
            {
                Assert.True(client.Timeout.TotalSeconds > 0 && client.Timeout.TotalSeconds <= 120,
                    "HttpUpdateClient should have a reasonable default timeout (0 < timeout <= 120s)");
            }
        }
    }

    public class PluginPathResolverTests
    {
        [Fact]
        public void UsesAssemblyLocationWhenAvailable()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                string result = PluginPathResolver.ResolvePluginPackagePath(tempFile, Path.GetTempPath());
                Assert.Equal(tempFile, result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void FallsBackToPluginsDirectory()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                string result = PluginPathResolver.ResolvePluginPackagePath(null, tempDir);
                string expected = Path.Combine(tempDir, "Plugins", "KeePassAutoReload.dll");
                Assert.Equal(expected, result);
                Assert.True(Directory.Exists(Path.Combine(tempDir, "Plugins")), "fallback should create Plugins directory");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void FallsBackWhenAssemblyLocationDoesNotExist()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                string missingPath = Path.Combine(tempDir, "missing", "KeePassAutoReload.dll");
                string result = PluginPathResolver.ResolvePluginPackagePath(missingPath, tempDir);
                string expected = Path.Combine(tempDir, "Plugins", "KeePassAutoReload.dll");
                Assert.Equal(expected, result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void FallsBackWhenAssemblyLocationIsEmptyOrWhitespace()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                string result = PluginPathResolver.ResolvePluginPackagePath("   ", tempDir);
                string expected = Path.Combine(tempDir, "Plugins", "KeePassAutoReload.dll");
                Assert.Equal(expected, result);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ThrowsWhenKeePassDirectoryIsNullOrWhitespace(string keepassDirectory)
        {
            Assert.Throws<ArgumentException>(() => PluginPathResolver.ResolvePluginPackagePath(null, keepassDirectory));
        }
    }

    internal sealed class FakeUpdateClient : IUpdateClient
    {
        public string Response { get; set; }
        public byte[] FileData { get; set; }
        public string LastDownloadDestination { get; private set; }
        public CancellationToken? LastCancellationToken { get; private set; }

        public Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default)
        {
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Response ?? string.Empty);
        }

        public Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            LastDownloadDestination = destinationPath;
            LastCancellationToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            if (FileData != null)
            {
                File.WriteAllBytes(destinationPath, FileData);
            }
            return Task.CompletedTask;
        }
    }
}
