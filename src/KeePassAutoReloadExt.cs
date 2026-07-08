using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeePass.DataExchange;
using KeePass.Plugins;
using KeePassLib;
using KeePassLib.Interfaces;

namespace KeePassAutoReload
{
    public sealed class KeePassAutoReloadExt : Plugin
    {
        private const string ProductName = "KeePass Auto Reload";
        private const string ConfigEnabled = "KeePassAutoReload.Enabled";
        private const string ConfigIntervalSeconds = "KeePassAutoReload.IntervalSeconds";
        private const string ConfigSkipModified = "KeePassAutoReload.SkipModified";
        private const int DefaultIntervalSeconds = 30;
        private const int MinimumIntervalSeconds = 10;

        private IPluginHost m_host;
        private System.Windows.Forms.Timer m_timer;
        private ToolStripMenuItem m_enableItem;
        private ToolStripMenuItem m_skipModifiedItem;
        private ToolStripMenuItem m_intervalItem;
        private bool m_syncInProgress;
        private PluginPackageFormat m_packageFormat;

        public override bool Initialize(IPluginHost host)
        {
            m_host = host;
            m_packageFormat = ResolveInstalledFormat();
            m_timer = new System.Windows.Forms.Timer();
            m_timer.Tick += OnTimerTick;
            ConfigureTimer();

            if (IsEnabled()) m_timer.Start();
            StartAutoUpdateCheck();
            return true;
        }

        public override ToolStripMenuItem GetMenuItem(PluginMenuType t)
        {
            if (t != PluginMenuType.Main) return null;

            ToolStripMenuItem root = new ToolStripMenuItem(ProductName);

            ToolStripMenuItem syncNow = new ToolStripMenuItem("Synchronize Now");
            syncNow.Click += OnSynchronizeNow;
            root.DropDownItems.Add(syncNow);

            m_enableItem = new ToolStripMenuItem("Enable Auto Sync");
            m_enableItem.CheckOnClick = true;
            m_enableItem.Checked = IsEnabled();
            m_enableItem.Click += OnToggleEnabled;
            root.DropDownItems.Add(m_enableItem);

            m_skipModifiedItem = new ToolStripMenuItem("Skip When Database Has Unsaved Changes");
            m_skipModifiedItem.CheckOnClick = true;
            m_skipModifiedItem.Checked = GetSkipModified();
            m_skipModifiedItem.Click += OnToggleSkipModified;
            root.DropDownItems.Add(m_skipModifiedItem);

            m_intervalItem = new ToolStripMenuItem();
            root.DropDownItems.Add(m_intervalItem);
            AddIntervalItems(m_intervalItem);
            UpdateIntervalText();

            root.DropDownItems.Add(new ToolStripSeparator());

            ToolStripMenuItem updateItem = new ToolStripMenuItem("Check for Updates");
            updateItem.Click += OnCheckForUpdates;
            root.DropDownItems.Add(updateItem);

            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About " + ProductName);
            aboutItem.Click += OnAbout;
            root.DropDownItems.Add(aboutItem);

            return root;
        }

        private void OnSynchronizeNow(object sender, EventArgs e)
        {
            Synchronize(false, true);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            Synchronize(GetSkipModified(), false);
        }

        private void OnAbout(object sender, EventArgs e)
        {
            string message = PluginAboutInfo.BuildText(UpdateChecker.GetCurrentVersion(), GetIntervalSeconds(), GetSkipModified());
            MessageBox.Show(GetOwner(), message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnCheckForUpdates(object sender, EventArgs e)
        {
            CheckForUpdatesAsync(true);
        }

        private void StartAutoUpdateCheck()
        {
            Task.Run(async () =>
            {
                await Task.Delay(4000);
                await CheckForUpdatesAsync(false);
            });
        }

        private Task CheckForUpdatesAsync(bool interactive)
        {
            return Task.Run(async () => await CheckForUpdates(interactive));
        }

        private async Task CheckForUpdates(bool interactive)
        {
            try
            {
                UpdateInfo info = await UpdateChecker.CheckLatestAsync(m_packageFormat);

                if (info == null || !info.IsUpdateAvailable)
                {
                    if (interactive)
                    {
                        ShowOnUi(delegate
                        {
                            MessageBox.Show(GetOwner(),
                                "You are already using the latest version: " + UpdateChecker.GetCurrentVersion(),
                                ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        });
                    }
                    return;
                }

                ShowOnUi(delegate
                {
                    PromptForUpdate(info);
                });
            }
            catch (Exception ex)
            {
                if (!interactive) return;

                ShowOnUi(delegate
                {
                    MessageBox.Show(GetOwner(), "Update check failed:\r\n" + ex.Message,
                        ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
            }
        }

        private void PromptForUpdate(UpdateInfo info)
        {
            string message = "A new version of KeePass Auto Reload is available.\r\n\r\n" +
                "Current version: " + UpdateChecker.GetCurrentVersion() + "\r\n" +
                "Latest version: " + info.LatestVersion + "\r\n\r\n" +
                "Download and install the update now?";

            DialogResult result = MessageBox.Show(GetOwner(), message, ProductName + " Update",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                InstallUpdateAsync(info);
            }
        }

        private void InstallUpdateAsync(UpdateInfo info)
        {
            Task.Run(async () => await InstallUpdate(info));
        }

        private async Task InstallUpdate(UpdateInfo info)
        {
            try
            {
                string targetPath = GetPluginPackagePath();
                string tempPath = targetPath + ".download";

                using (HttpUpdateClient client = new HttpUpdateClient())
                {
                    await client.DownloadFileAsync(info.AssetUrl, tempPath);
                }

                try
                {
                    File.Copy(tempPath, targetPath, true);
                    File.Delete(tempPath);

                    ShowOnUi(delegate
                    {
                        MessageBox.Show(GetOwner(),
                            "KeePass Auto Reload has been updated. Restart KeePass to use the new version.",
                            ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    });
                }
                catch (Exception copyEx)
                {
                    string pendingPath = targetPath + ".new";
                    File.Copy(tempPath, pendingPath, true);
                    File.Delete(tempPath);

                    bool updaterScheduled = TryScheduleUpdater(targetPath, pendingPath);

                    ShowOnUi(delegate
                    {
                        string message;
                        if (updaterScheduled)
                        {
                            message = "The update was downloaded and will be installed when KeePass exits.\r\n" +
                                "KeePass will restart automatically after the update is applied.";
                        }
                        else
                        {
                            message = "The update was downloaded, but the active plugin file could not be replaced.\r\n" +
                                "New file: " + pendingPath + "\r\n" +
                                "Reason: " + copyEx.Message;
                        }
                        MessageBox.Show(GetOwner(), message,
                            ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    });
                }
            }
            catch (Exception ex)
            {
                ShowOnUi(delegate
                {
                    MessageBox.Show(GetOwner(), "Update download failed:\r\n" + ex.Message,
                        ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                });
            }
        }

        private static string GetPluginPackagePath()
        {
            return PluginPathResolver.ResolvePluginPackagePath(
                typeof(KeePassAutoReloadExt).Assembly.Location,
                Path.GetDirectoryName(Application.ExecutablePath));
        }

        private PluginPackageFormat ResolveInstalledFormat()
        {
            try
            {
                return PluginPathResolver.ResolveInstalledFormat(Path.GetDirectoryName(Application.ExecutablePath));
            }
            catch
            {
                return PluginPackageFormat.Dll;
            }
        }

        private bool TryScheduleUpdater(string pluginPath, string newPluginPath)
        {
            try
            {
                string updaterPath = Path.Combine(Path.GetDirectoryName(pluginPath), "KeePassAutoReload.Updater.exe");
                return PluginUpdater.TryScheduleUpdate(
                    pluginPath,
                    newPluginPath,
                    updaterPath,
                    Process.GetCurrentProcess().Id,
                    Application.ExecutablePath,
                    new ProcessStarter());
            }
            catch
            {
                return false;
            }
        }

        private void ShowOnUi(MethodInvoker action)
        {
            Form owner = GetOwner();
            if (owner != null && !owner.IsDisposed)
            {
                if (owner.InvokeRequired) owner.BeginInvoke(action);
                else action();
            }
            else
            {
                action();
            }
        }

        private Form GetOwner()
        {
            return (m_host != null) ? m_host.MainWindow : null;
        }

        private void Synchronize(bool skipWhenModified, bool showResult)
        {
            if (m_syncInProgress || !SyncGuard.CanRunSync(m_host != null, m_host.Database != null, m_host.MainWindow != null)) return;

            PwDatabase database = m_host.Database;
            if (!AutoSyncPolicy.ShouldRun(database.IsOpen, database.Modified, skipWhenModified))
            {
                if (showResult)
                {
                    MessageBox.Show("Database is not open or has unsaved changes.",
                        ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            try
            {
                m_syncInProgress = true;
                bool? result = ImportUtil.Synchronize(database, (IUIOperations)m_host.MainWindow,
                    database.IOConnectionInfo, false, m_host.MainWindow);

                m_host.MainWindow.UpdateUI(false, null, true, null, true, null, false);

                if (showResult)
                {
                    string message = (result.HasValue && result.Value) ?
                        "Synchronize completed." :
                        "Synchronize finished without changes.";
                    MessageBox.Show(message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (showResult)
                {
                    MessageBox.Show("Synchronize failed:\r\n" + ex.Message,
                        ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                m_syncInProgress = false;
            }
        }

        private void OnToggleEnabled(object sender, EventArgs e)
        {
            SetEnabled(m_enableItem.Checked);
            if (m_enableItem.Checked) m_timer.Start();
            else m_timer.Stop();
            SaveConfig();
        }

        private void OnToggleSkipModified(object sender, EventArgs e)
        {
            m_host.CustomConfig.SetBool(ConfigSkipModified, m_skipModifiedItem.Checked);
            SaveConfig();
        }

        private void AddIntervalItems(ToolStripMenuItem parent)
        {
            AddIntervalItem(parent, 10);
            AddIntervalItem(parent, 30);
            AddIntervalItem(parent, 60);
            AddIntervalItem(parent, 300);
        }

        private void AddIntervalItem(ToolStripMenuItem parent, int seconds)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(seconds + " seconds");
            item.Tag = seconds;
            item.Click += OnIntervalSelected;
            parent.DropDownItems.Add(item);
        }

        private void OnIntervalSelected(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null) return;

            int seconds = (int)item.Tag;
            m_host.CustomConfig.SetLong(ConfigIntervalSeconds, seconds);
            ConfigureTimer();
            UpdateIntervalText();
            SaveConfig();
        }

        private void ConfigureTimer()
        {
            int seconds = GetIntervalSeconds();
            m_timer.Interval = seconds * 1000;
        }

        private int GetIntervalSeconds()
        {
            long configured = m_host.CustomConfig.GetLong(ConfigIntervalSeconds, DefaultIntervalSeconds);
            if (configured < MinimumIntervalSeconds) configured = MinimumIntervalSeconds;
            if (configured > int.MaxValue / 1000) configured = DefaultIntervalSeconds;
            return (int)configured;
        }

        private bool IsEnabled()
        {
            return m_host.CustomConfig.GetBool(ConfigEnabled, false);
        }

        private void SetEnabled(bool enabled)
        {
            m_host.CustomConfig.SetBool(ConfigEnabled, enabled);
        }

        private bool GetSkipModified()
        {
            return m_host.CustomConfig.GetBool(ConfigSkipModified, true);
        }

        private void UpdateIntervalText()
        {
            if (m_intervalItem == null) return;
            m_intervalItem.Text = "Interval: " + GetIntervalSeconds() + " seconds";

            foreach (ToolStripItem child in m_intervalItem.DropDownItems)
            {
                ToolStripMenuItem item = child as ToolStripMenuItem;
                if (item == null) continue;
                item.Checked = ((int)item.Tag == GetIntervalSeconds());
            }
        }

        private void SaveConfig()
        {
            if (m_host != null && m_host.MainWindow != null) m_host.MainWindow.SaveConfig();
        }

        public override void Terminate()
        {
            if (m_timer != null)
            {
                m_timer.Stop();
                m_timer.Tick -= OnTimerTick;
                m_timer.Dispose();
                m_timer = null;
            }
        }
    }
}
