using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace PSKCharms
{
    public class Program : Application
    {
        private static Mutex _appMutex;
        private static EventWaitHandle _wakeUpEvent;
        private AppSettings _settings;
        private CharmCatalog _catalog;
        private OverlayWindow _overlay;
        private ControlCenterWindow _controlCenter;
        private TrayManager _tray;

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                    File.AppendAllText(logPath, DateTime.UtcNow.ToString("o") + " [AppDomain] " + e.ExceptionObject.ToString() + Environment.NewLine);
                }
                catch { }
            };

            bool createdNew;
            _appMutex = new Mutex(true, "PSK_Charms_Desktop_Mutex_SingleInstance", out createdNew);

            if (!createdNew)
            {
                // Signal the already running instance to bring its Control Center to the front!
                try
                {
                    using (var evt = EventWaitHandle.OpenExisting("PSK_Charms_Wakeup_Signal_Event"))
                    {
                        evt.Set();
                    }
                }
                catch { }

                // Check if existing window is present and can be brought to front
                IntPtr hWnd = Win32Helper.FindWindow(null, "PSK Charms • Created by PSK Developer");
                if (hWnd != IntPtr.Zero)
                {
                    Win32Helper.ShowWindow(hWnd, 9); // SW_RESTORE
                    Win32Helper.SetForegroundWindow(hWnd);
                    return;
                }

                // If window not yet restored, give existing instance 400ms to open
                Thread.Sleep(400);
                hWnd = Win32Helper.FindWindow(null, "PSK Charms • Created by PSK Developer");
                if (hWnd != IntPtr.Zero)
                {
                    Win32Helper.ShowWindow(hWnd, 9);
                    Win32Helper.SetForegroundWindow(hWnd);
                    return;
                }

                // If still no window can be restored, the previous instance is an orphaned background/zombie process.
                // Clean it up so the user is NEVER locked out!
                try
                {
                    int currentPid = Process.GetCurrentProcess().Id;
                    foreach (var proc in Process.GetProcessesByName("PSK"))
                    {
                        if (proc.Id != currentPid)
                        {
                            try
                            {
                                proc.Kill();
                                proc.WaitForExit(1000);
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                // Re-acquire mutex cleanly for this fresh launch
                try
                {
                    _appMutex = new Mutex(true, "PSK_Charms_Desktop_Mutex_SingleInstance", out createdNew);
                }
                catch { }
            }

            try
            {
                _wakeUpEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "PSK_Charms_Wakeup_Signal_Event");

                var app = new Program();
                app.DispatcherUnhandledException += (s, e) =>
                {
                    try
                    {
                        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                        File.AppendAllText(logPath, DateTime.UtcNow.ToString("o") + " [Dispatcher] " + e.Exception.ToString() + Environment.NewLine);
                    }
                    catch { }
                    e.Handled = true; // Prevent crash!
                };
                app.Run();
            }
            catch (Exception ex)
            {
                try
                {
                    string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                    File.AppendAllText(logPath, DateTime.UtcNow.ToString("o") + " [Main] " + ex.ToString() + Environment.NewLine);
                }
                catch { }
            }
        }

        private static void Log(string msg)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diag.log");
                File.AppendAllText(logPath, DateTime.UtcNow.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine);
            }
            catch { }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Log("OnStartup started");
                base.OnStartup(e);

                ShutdownMode = ShutdownMode.OnExplicitShutdown;

                // Load configuration and assets catalog
                Log("Loading settings and catalog");
                _settings = AppSettings.Load();
                _catalog = CharmCatalog.Load();
                Log("Loaded " + _catalog.Charms.Count + " charms");

                // Create desktop overlay
                Log("Creating OverlayWindow");
                _overlay = new OverlayWindow(_settings, _catalog);

                // Create control center window
                Log("Creating ControlCenterWindow");
                _controlCenter = new ControlCenterWindow(_settings, _catalog, _overlay);

                // Wire overlay events
                _overlay.RequestOpenControlCenter += ShowControlCenter;
                _overlay.RequestNextCharm += () => _controlCenter.SelectNextCharm();
                _overlay.RequestExit += ExitApplication;

                // Create system tray icon
                Log("Creating TrayManager");
                _tray = new TrayManager(
                    _settings,
                    onOpenControlCenter: ShowControlCenter,
                    onNextCharm: () => _controlCenter.SelectNextCharm(),
                    onResetPosition: () =>
                    {
                        _settings.AnchorPercentX = 0.5;
                        _settings.Save();
                        _overlay.ReloadSettings();
                    },
                    onToggleBreeze: (enabled) =>
                    {
                        _settings.AmbientBreeze = enabled;
                        _settings.Save();
                    },
                    onToggleAlwaysOnTop: (enabled) =>
                    {
                        _settings.AlwaysOnTop = enabled;
                        _settings.Save();
                        _overlay.ReloadSettings();
                    },
                    onExit: ExitApplication);

                MainWindow = _controlCenter;

                // Start background thread to listen for wakeup signals from future instances
                var wakeupThread = new Thread(() =>
                {
                    while (_wakeUpEvent != null)
                    {
                        try
                        {
                            _wakeUpEvent.WaitOne();
                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                ShowControlCenter();
                            }));
                        }
                        catch { break; }
                    }
                }) { IsBackground = true };
                wakeupThread.Start();

                // Show overlay on desktop
                Log("Showing OverlayWindow");
                _overlay.Show();

                // On first run or if launched directly, show Control Center to introduce features
                Log("Showing ControlCenterWindow");
                _controlCenter.Show();
                _controlCenter.Activate();
                _controlCenter.Focus();
                Log("OnStartup completed successfully");
            }
            catch (Exception ex)
            {
                Log("CRITICAL ERROR IN ONSTARTUP: " + ex.ToString());
            }
        }

        private void ShowControlCenter()
        {
            if (_controlCenter != null)
            {
                _controlCenter.Show();
                if (_controlCenter.WindowState == WindowState.Minimized)
                {
                    _controlCenter.WindowState = WindowState.Normal;
                }
                _controlCenter.Activate();
                _controlCenter.Topmost = true;
                _controlCenter.Topmost = false;
                _controlCenter.Focus();

                try
                {
                    var hwnd = new WindowInteropHelper(_controlCenter).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        Win32Helper.ShowWindow(hwnd, 9); // SW_RESTORE
                        Win32Helper.SetForegroundWindow(hwnd);
                    }
                }
                catch { }
            }
        }

        private void ExitApplication()
        {
            Log("ExitApplication called");
            if (_tray != null)
            {
                _tray.Dispose();
                _tray = null;
            }

            if (_overlay != null)
            {
                _overlay.Close();
                _overlay = null;
            }

            if (_controlCenter != null)
            {
                _controlCenter.Close();
                _controlCenter = null;
            }

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log("OnExit called! Exit code: " + e.ApplicationExitCode);
            if (_tray != null)
            {
                _tray.Dispose();
                _tray = null;
            }

            if (_appMutex != null)
            {
                try { _appMutex.ReleaseMutex(); } catch { }
                _appMutex.Dispose();
            }

            base.OnExit(e);
        }
    }
}
