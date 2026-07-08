using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KeePassAutoReload
{
    internal sealed class UpdateInfo
    {
        public string LatestVersion;
        public string ReleaseUrl;
        public string AssetUrl;
        public string ChecksumUrl;
        public bool IsUpdateAvailable;
    }

    internal interface IUpdateClient
    {
        Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default);
        Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default);
    }

    internal sealed class HttpUpdateClient : IUpdateClient, IDisposable
    {
        private readonly HttpClient _client;
        private readonly HttpClientHandler _handler;

        internal HttpClientHandler Handler { get { return _handler; } }
        internal TimeSpan Timeout { get { return _client.Timeout; } }

        public HttpUpdateClient()
        {
            _handler = new HttpClientHandler();
            _handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            _client = new HttpClient(_handler);
            _client.Timeout = TimeSpan.FromSeconds(30);
            _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "KeePassAutoReload");
            _client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        }

        public async Task<string> DownloadStringAsync(string url, CancellationToken cancellationToken = default)
        {
            using (HttpResponseMessage response = await _client.GetAsync(url, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken = default)
        {
            using (HttpResponseMessage response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                byte[] data = await response.Content.ReadAsByteArrayAsync();
                File.WriteAllBytes(destinationPath, data);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
            _handler.Dispose();
        }
    }

    internal static class UpdateChecker
    {
        public const string ReleasesApiUrl = "https://api.github.com/repos/hieuck/KeePassAutoReload/releases";
        public const string ReleasesUrl = "https://github.com/hieuck/KeePassAutoReload/releases";

        public static string GetCurrentVersion()
        {
            Assembly assembly = typeof(UpdateChecker).Assembly;
            object[] attrs = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            if (attrs.Length > 0)
            {
                AssemblyInformationalVersionAttribute attr = (AssemblyInformationalVersionAttribute)attrs[0];
                if (!string.IsNullOrEmpty(attr.InformationalVersion)) return attr.InformationalVersion;
            }

            Version version = assembly.GetName().Version;
            return (version != null) ? version.ToString(3) : "0.0.0";
        }

        public static bool IsNewerVersion(string currentVersion, string candidateVersion)
        {
            Version current;
            Version candidate;
            if (!TryParseVersion(currentVersion, out current)) return false;
            if (!TryParseVersion(candidateVersion, out candidate)) return false;

            return candidate.CompareTo(current) > 0;
        }

        public static async Task<UpdateInfo> CheckLatestAsync(IUpdateClient client, CancellationToken cancellationToken = default)
        {
            return await CheckLatestAsync(client, PluginPackageFormat.Dll, cancellationToken);
        }

        public static async Task<UpdateInfo> CheckLatestAsync(IUpdateClient client, PluginPackageFormat format, CancellationToken cancellationToken = default)
        {
            if (client == null) throw new ArgumentNullException("client");

            cancellationToken.ThrowIfCancellationRequested();
            string json = await client.DownloadStringAsync(ReleasesApiUrl, cancellationToken);
            string tagName = GetNewestVersionTag(ExtractJsonStrings(json, "tag_name").ToArray());

            UpdateInfo info = new UpdateInfo();
            info.LatestVersion = tagName;
            info.ReleaseUrl = BuildReleaseUrl(tagName);
            info.AssetUrl = BuildAssetUrl(tagName, format);
            info.ChecksumUrl = BuildChecksumUrl(tagName);
            info.IsUpdateAvailable = IsNewerVersion(GetCurrentVersion(), tagName);
            return info;
        }

        public static async Task<UpdateInfo> CheckLatestAsync(CancellationToken cancellationToken = default)
        {
            return await CheckLatestAsync(PluginPackageFormat.Dll, cancellationToken);
        }

        public static async Task<UpdateInfo> CheckLatestAsync(PluginPackageFormat format, CancellationToken cancellationToken = default)
        {
            using (HttpUpdateClient client = new HttpUpdateClient())
            {
                return await CheckLatestAsync(client, format, cancellationToken);
            }
        }

        public static string GetNewestVersionTag(string[] tags)
        {
            string newestTag = string.Empty;
            Version newestVersion = null;

            foreach (string tag in tags)
            {
                Version version;
                if (!TryParseVersion(tag, out version)) continue;

                if (newestVersion == null || version.CompareTo(newestVersion) > 0)
                {
                    newestVersion = version;
                    newestTag = tag;
                }
            }

            return newestTag;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(value)) return false;

            string normalized = value.Trim();
            if (normalized.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(1);

            int metadataIndex = normalized.IndexOf('+');
            if (metadataIndex >= 0)
                normalized = normalized.Substring(0, metadataIndex);

            return Version.TryParse(normalized, out version);
        }

        private static string BuildAssetUrl(string tagName, PluginPackageFormat format)
        {
            if (string.IsNullOrEmpty(tagName)) return ReleasesUrl;
            string extension = format == PluginPackageFormat.Plgx ? ".plgx" : ".dll";
            return "https://github.com/hieuck/KeePassAutoReload/releases/download/" + tagName + "/KeePassAutoReload" + extension;
        }

        private static string BuildReleaseUrl(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return ReleasesUrl;
            return ReleasesUrl + "/tag/" + tagName;
        }

        private static string BuildChecksumUrl(string tagName)
        {
            if (string.IsNullOrEmpty(tagName)) return ReleasesUrl;
            return "https://github.com/hieuck/KeePassAutoReload/releases/download/" + tagName + "/SHA256SUMS.txt";
        }

        private static List<string> ExtractJsonStrings(string json, string name)
        {
            List<string> values = new List<string>();
            if (string.IsNullOrEmpty(json)) return values;

            MatchCollection matches = Regex.Matches(json, "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"");
            foreach (Match match in matches)
            {
                if (match.Success) values.Add(Regex.Unescape(match.Groups["value"].Value));
            }

            return values;
        }
    }
}
