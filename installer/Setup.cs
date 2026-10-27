using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace PSKSetup
{
    public class SetupWindow : Window
    {
        private ProgressBar _progressBar;
        private TextBlock _statusText;
        private TextBlock _titleText;
        private TextBlock _subTitleText;
        private Button _actionBtn;

        public SetupWindow()
        {
            Title = "PSK Charms Setup • Created by PSK Developer";
            Width = 480;
            Height = 340;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            BuildUI();
            Loaded += (s, e) => StartSetup();
        }

        private void BuildUI()
        {
            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(17, 18, 28)), // #11121C
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 215, 0)), // Golden outline
                BorderThickness = new Thickness(1.5),
                ClipToBounds = true
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });  // Titlebar / Drag
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(110) }); // Logo / Hero
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });  // Action button / footer

            // Drag handle
            var dragBar = new Border { Background = Brushes.Transparent };
            dragBar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
            Grid.SetRow(dragBar, 0);
            grid.Children.Add(dragBar);

            // Close button
            var closeBtn = new Button
            {
                Content = "✕",
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 12, 0),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            closeBtn.Click += (s, e) => Close();
            Grid.SetRow(closeBtn, 0);
            grid.Children.Add(closeBtn);

            // Hero section with Avatar
            var heroStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var avatarBorder = new Border
            {
                Width = 72,
                Height = 72,
                CornerRadius = new CornerRadius(36),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                BorderThickness = new Thickness(2.5),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using (var stream = asm.GetManifestResourceStream("avatar.png"))
                {
                    if (stream != null)
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = stream;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        avatarBorder.Child = new Image { Source = bmp, Stretch = Stretch.UniformToFill };
                    }
                }
            }
            catch { }

            heroStack.Children.Add(avatarBorder);

            _titleText = new TextBlock
            {
                Text = "PSK Charms",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            heroStack.Children.Add(_titleText);

            _subTitleText = new TextBlock
            {
                Text = "Interactive Desktop Companion • Created by PSK Developer",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 180)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            heroStack.Children.Add(_subTitleText);

            Grid.SetRow(heroStack, 1);
            grid.Children.Add(heroStack);

            // Content section: progress bar & status
            var contentStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(36, 0, 36, 0)
            };

            _statusText = new TextBlock
            {
                Text = "Preparing installation...",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 235)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            contentStack.Children.Add(_statusText);

            _progressBar = new ProgressBar
            {
                Height = 8,
                Minimum = 0,
                Maximum = 100,
                Value = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Background = new SolidColorBrush(Color.FromRgb(35, 38, 55)),
                BorderThickness = new Thickness(0)
            };
            contentStack.Children.Add(_progressBar);

            Grid.SetRow(contentStack, 2);
            grid.Children.Add(contentStack);

            // Action button
            var footerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
                VerticalAlignment = VerticalAlignment.Stretch
            };

            _actionBtn = new Button
            {
                Content = "Setting up...",
                Width = 200,
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                Foreground = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                IsEnabled = false
            };
            _actionBtn.Click += (s, e) => LaunchAndClose();

            footerBorder.Child = _actionBtn;
            Grid.SetRow(footerBorder, 3);
            grid.Children.Add(footerBorder);

            mainBorder.Child = grid;
            Content = mainBorder;
        }

        private void StartSetup()
        {
            var thread = new Thread(() =>
            {
                try
                {
                    UpdateStatus("Extracting charm assets...", 30);
                    Thread.Sleep(300);

                    // Destination folder: %LocalAppData%\Programs\PSK_Charms
                    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string targetDir = Path.Combine(localAppData, "Programs", "PSK_Charms");

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // Extract embedded ZIP archive
                    var asm = Assembly.GetExecutingAssembly();
                    string tempZip = Path.Combine(Path.GetTempPath(), "PSK_Payload_" + Guid.NewGuid().ToString("N") + ".zip");

                    using (var resStream = asm.GetManifestResourceStream("payload.zip"))
                    {
                        if (resStream == null)
                        {
                            throw new Exception("Embedded installation payload not found.");
                        }

                        using (var fileStream = File.Create(tempZip))
                        {
                            resStream.CopyTo(fileStream);
                        }
                    }

                    UpdateStatus("Installing 72+ charms and physics engine...", 60);

                    // Unzip entries with overwrite
                    using (var archive = ZipFile.OpenRead(tempZip))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            // Strip root folder if archive was wrapped in "PSK_Charms/"
                            string entryName = entry.FullName;
                            if (entryName.StartsWith("PSK_Charms/", StringComparison.OrdinalIgnoreCase) ||
                                entryName.StartsWith("PSK_Charms\\", StringComparison.OrdinalIgnoreCase))
                            {
                                entryName = entryName.Substring(11);
                            }

                            if (string.IsNullOrEmpty(entryName)) continue;

                            string outPath = Path.Combine(targetDir, entryName);
                            if (string.IsNullOrEmpty(entry.Name))
                            {
                                Directory.CreateDirectory(outPath);
                            }
                            else
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                                entry.ExtractToFile(outPath, true);
                                try { DeleteFile(outPath + ":Zone.Identifier"); } catch { }
                            }
                        }
                    }

                    try { File.Delete(tempZip); } catch { }

                    UpdateStatus("Creating Desktop and Start Menu shortcuts...", 85);

                    string exePath = Path.Combine(targetDir, "PSK.exe");
                    string iconPath = Path.Combine(targetDir, "Assets", "psk.ico");

                    // 1. Desktop Shortcut
                    try
                    {
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        string desktopLink = Path.Combine(desktopPath, "PSK Charms.lnk");
                        CreateShortcut(desktopLink, exePath, targetDir, iconPath, "PSK Charms - Interactive Desktop Companion Created by PSK Developer");
                    }
                    catch { }

                    // 2. Start Menu Shortcut
                    try
                    {
                        string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
                        string startLink = Path.Combine(startMenuPath, "PSK Charms.lnk");
                        CreateShortcut(startLink, exePath, targetDir, iconPath, "PSK Charms - Interactive Desktop Companion Created by PSK Developer");
                    }
                    catch { }

                    UpdateStatus("Installation complete! Launching PSK Charms...", 100);

                    // Launch application
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _actionBtn.IsEnabled = true;
                        _actionBtn.Content = "Launch PSK Charms";
                        _statusText.Text = "Ready! Starting desktop charm...";
                    }));

                    Thread.Sleep(800);

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        LaunchAndClose();
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _statusText.Text = "Setup notice: " + ex.Message;
                        _progressBar.Value = 100;
                        _actionBtn.IsEnabled = true;
                        _actionBtn.Content = "Close";
                    }));
                }
            }) { IsBackground = true };

            thread.Start();
        }

        private void UpdateStatus(string message, double progress)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                _statusText.Text = message;
                _progressBar.Value = progress;
            }));
        }

        private void LaunchAndClose()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string exePath = Path.Combine(localAppData, "Programs", "PSK_Charms", "PSK.exe");
                if (File.Exists(exePath))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath)
                    };
                    Process.Start(psi);
                }
            }
            catch { }

            Close();
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath, string description)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                Type shortcutType = shortcut.GetType();
                shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
                shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDir });
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
                }
                if (!string.IsNullOrEmpty(description))
                {
                    shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
                }
                shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteFile(string lpFileName);
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Application();
            app.Run(new SetupWindow());
        }
    }
}
