using System;
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
        private Timer m_timer;
        private ToolStripMenuItem m_enableItem;
        private ToolStripMenuItem m_skipModifiedItem;
        private ToolStripMenuItem m_intervalItem;
        private bool m_syncInProgress;

        public override bool Initialize(IPluginHost host)
        {
            m_host = host;
            m_timer = new Timer();
            m_timer.Tick += OnTimerTick;
            ConfigureTimer();

            if (IsEnabled()) m_timer.Start();
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

        private void Synchronize(bool skipWhenModified, bool showResult)
        {
            if (m_syncInProgress || m_host == null || m_host.Database == null) return;

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
