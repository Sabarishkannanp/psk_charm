using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PSKCharms
{
    public class TrayManager : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private AppSettings _settings;
        private Action _onOpenControlCenter;
        private Action _onNextCharm;
        private Action _onResetPosition;
        private Action _onExit;
        private Action<bool> _onToggleBreeze;
        private Action<bool> _onToggleAlwaysOnTop;

        public TrayManager(
            AppSettings settings,
            Action onOpenControlCenter,
            Action onNextCharm,
            Action onResetPosition,
            Action<bool> onToggleBreeze,
            Action<bool> onToggleAlwaysOnTop,
            Action onExit)
        {
            _settings = settings;
            _onOpenControlCenter = onOpenControlCenter;
            _onNextCharm = onNextCharm;
            _onResetPosition = onResetPosition;
            _onToggleBreeze = onToggleBreeze;
            _onToggleAlwaysOnTop = onToggleAlwaysOnTop;
            _onExit = onExit;

            InitializeTray();
        }

        private void InitializeTray()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "PSK Charms - Created by PSK Developer";

            // Load custom icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "psk.ico");
                if (File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    _notifyIcon.Icon = SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }

            // Click behavior
            _notifyIcon.Click += (s, e) =>
            {
                var me = e as MouseEventArgs;
                if (me != null && me.Button == MouseButtons.Left)
                {
                    if (_onOpenControlCenter != null) _onOpenControlCenter();
                }
            };

            _notifyIcon.DoubleClick += (s, e) =>
            {
                if (_onOpenControlCenter != null) _onOpenControlCenter();
            };

            BuildContextMenu();
            _notifyIcon.Visible = true;

            // Show introductory balloon tip
            try
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    "PSK Charms Active",
                    "Created by PSK Developer • Drag your charm to swing, or click this icon to customize!",
                    ToolTipIcon.Info);
            }
            catch { }
        }

        public void BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var titleItem = new ToolStripMenuItem("PSK Charms • PSK Developer")
            {
                Enabled = false,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            menu.Items.Add(titleItem);
            menu.Items.Add(new ToolStripSeparator());

            var openItem = new ToolStripMenuItem("⚙️ Open Control Center & Catalog", null, (s, e) => { if (_onOpenControlCenter != null) _onOpenControlCenter(); })
            {
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            menu.Items.Add(openItem);

            var nextItem = new ToolStripMenuItem("🔀 Next Charm", null, (s, e) => { if (_onNextCharm != null) _onNextCharm(); });
            menu.Items.Add(nextItem);

            var resetItem = new ToolStripMenuItem("📍 Reset Position to Center", null, (s, e) => { if (_onResetPosition != null) _onResetPosition(); });
            menu.Items.Add(resetItem);

            menu.Items.Add(new ToolStripSeparator());

            var breezeItem = new ToolStripMenuItem("🌬️ Idle Breeze Swaying")
            {
                CheckOnClick = true,
                Checked = _settings.AmbientBreeze
            };
            breezeItem.Click += (s, e) =>
            {
                _settings.AmbientBreeze = breezeItem.Checked;
                _settings.Save();
                if (_onToggleBreeze != null) _onToggleBreeze(breezeItem.Checked);
            };
            menu.Items.Add(breezeItem);

            var topItem = new ToolStripMenuItem("📌 Always On Top")
            {
                CheckOnClick = true,
                Checked = _settings.AlwaysOnTop
            };
            topItem.Click += (s, e) =>
            {
                _settings.AlwaysOnTop = topItem.Checked;
                _settings.Save();
                if (_onToggleAlwaysOnTop != null) _onToggleAlwaysOnTop(topItem.Checked);
            };
            menu.Items.Add(topItem);

            menu.Items.Add(new ToolStripSeparator());

            var aboutItem = new ToolStripMenuItem("ℹ️ About PSK...", null, (s, e) =>
            {
                if (_onOpenControlCenter != null) _onOpenControlCenter();
            });
            menu.Items.Add(aboutItem);

            var exitItem = new ToolStripMenuItem("❌ Exit PSK", null, (s, e) => { if (_onExit != null) _onExit(); });
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        public void ShowMessage(string title, string message)
        {
            try
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
            catch { }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
