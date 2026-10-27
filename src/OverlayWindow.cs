using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PSKCharms
{
    public class OverlayWindow : Window
    {
        private AppSettings _settings;
        private CharmCatalog _catalog;
        private PhysicsSimulator _simulator;

        private Canvas _rootCanvas;
        private Path _ropePath;
        private List<Ellipse> _beads;
        private Border _anchorElement;
        private Border _charmContainer;
        private Ellipse _glow;
        private double _attachX = 85.0;
        private double _attachY = 0.0;
        private double _imgHeight = 230.0;
        private Image _charmImage;
        private RotateTransform _charmRotateTransform;
        private ScaleTransform _charmScaleTransform;

        private bool _isDraggingAnchor = false;
        private double _anchorDragStartX;
        private DateTime _lastFrameTime;
        private double _baseCharmWidth = 170.0;
        private double _baseCharmHeight = 230.0;

        public event Action RequestOpenControlCenter;
        public event Action RequestNextCharm;
        public event Action RequestExit;

        public OverlayWindow(AppSettings settings, CharmCatalog catalog)
        {
            _settings = settings;
            _catalog = catalog;

            InitializeWindowProperties();
            InitializeUIElements();
            InitializePhysics();

            Loaded += OnWindowLoaded;
        }

        private void InitializeWindowProperties()
        {
            Title = "PSK Desktop Charm Overlay";
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = null; // Essential for click-through on empty areas
            Topmost = _settings.AlwaysOnTop;
            ShowInTaskbar = false;

            // Cover full virtual screen across multi-monitors
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            // Handle screen resolution changes
            SizeChanged += (s, e) =>
            {
                UpdateAnchorFromPercent();
            };
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Win32Helper.SetToolWindow(this);
            if (_settings.ClickThrough)
            {
                Win32Helper.SetClickThrough(this, true);
            }
            if (_settings.AlwaysOnTop)
            {
                Win32Helper.MakeTopMost(this);
            }

            _lastFrameTime = DateTime.UtcNow;
            CompositionTarget.Rendering += OnRenderingFrame;

            InstallScreenshotHook();
            Closed += (s, ev) =>
            {
                if (_keyboardHook != IntPtr.Zero) Win32Helper.UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            };
        }

        #region Screenshot support

        private IntPtr _keyboardHook = IntPtr.Zero;
        private Win32Helper.LowLevelKeyboardProc _keyboardProc;
        private System.Windows.Threading.DispatcherTimer _screenshotTimer;

        private void InstallScreenshotHook()
        {
            try
            {
                _keyboardProc = KeyboardHookCallback; // keep a reference so it is not garbage collected
                _keyboardHook = Win32Helper.SetWindowsHookEx(
                    Win32Helper.WH_KEYBOARD_LL, _keyboardProc, Win32Helper.GetModuleHandle(null), 0);
            }
            catch { }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)Win32Helper.WM_KEYDOWN || wParam == (IntPtr)Win32Helper.WM_SYSKEYDOWN))
            {
                int vk = System.Runtime.InteropServices.Marshal.ReadInt32(lParam);
                bool printScreen = vk == Win32Helper.VK_SNAPSHOT;
                bool winShiftS = vk == Win32Helper.VK_S &&
                                 (Win32Helper.GetAsyncKeyState(Win32Helper.VK_SHIFT) & 0x8000) != 0 &&
                                 ((Win32Helper.GetAsyncKeyState(Win32Helper.VK_LWIN) & 0x8000) != 0 ||
                                  (Win32Helper.GetAsyncKeyState(Win32Helper.VK_RWIN) & 0x8000) != 0);
                if (printScreen || winShiftS)
                {
                    Dispatcher.BeginInvoke((Action)(() => EnterScreenshotMode(15)));
                }
            }
            return Win32Helper.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        // Lets the overlay step aside (not topmost, fully click-through) so screenshot tools work normally.
        // The charm stays visible and is still captured.
        public void EnterScreenshotMode(int seconds)
        {
            Topmost = false;
            Win32Helper.SetClickThrough(this, true);

            if (_screenshotTimer == null)
            {
                _screenshotTimer = new System.Windows.Threading.DispatcherTimer();
                _screenshotTimer.Tick += (s, e) =>
                {
                    _screenshotTimer.Stop();
                    Topmost = _settings.AlwaysOnTop;
                    if (_settings.AlwaysOnTop) Win32Helper.MakeTopMost(this);
                    Win32Helper.SetClickThrough(this, _settings.ClickThrough);
                };
            }
            _screenshotTimer.Stop();
            _screenshotTimer.Interval = TimeSpan.FromSeconds(seconds);
            _screenshotTimer.Start();
        }

        #endregion

        private void InitializeUIElements()
        {
            _rootCanvas = new Canvas
            {
                Background = null,
                ClipToBounds = false
            };
            Content = _rootCanvas;

            // Rope path
            _ropePath = new Path
            {
                StrokeEndLineCap = PenLineCap.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };
            _rootCanvas.Children.Add(_ropePath);

            // Bead chain elements (pooled)
            _beads = new List<Ellipse>();
            for (int i = 0; i < 18; i++)
            {
                var bead = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Visibility = Visibility.Collapsed
                };
                _beads.Add(bead);
                _rootCanvas.Children.Add(bead);
            }

            // Top screen anchor clip - Minimal sleek translucent grip (invisible unless hovered, NO yellow box)
            _anchorElement = new Border
            {
                Width = 46,
                Height = 10,
                CornerRadius = new CornerRadius(0, 0, 5, 5),
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(200, 56, 189, 248)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.SizeWE,
                Opacity = 0.0, // Hidden by default so no intrusive box appears
                ToolTip = "Drag left/right to move charm position across screen"
            };
            _anchorElement.MouseEnter += (s, e) => { _anchorElement.Opacity = 0.85; };
            _anchorElement.MouseLeave += (s, e) => { if (!_isDraggingAnchor) _anchorElement.Opacity = 0.0; };
            _anchorElement.MouseLeftButtonDown += Anchor_MouseDown;
            _anchorElement.MouseMove += Anchor_MouseMove;
            _anchorElement.MouseLeftButtonUp += Anchor_MouseUp;
            _rootCanvas.Children.Add(_anchorElement);

            // Soft colored glow behind the charm (background overlay), below the charm in z-order
            _glow = new Ellipse
            {
                Width = _baseCharmWidth * 2.0,
                Height = _baseCharmHeight * 1.6,
                IsHitTestVisible = false,
                Visibility = _settings.GlowBackdrop ? Visibility.Visible : Visibility.Collapsed
            };
            _rootCanvas.Children.Add(_glow);

            // Charm container
            _charmContainer = new Border
            {
                Width = _baseCharmWidth,
                Height = _baseCharmHeight,
                Background = Brushes.Transparent, // Hit-testable
                Cursor = Cursors.Hand,
                ToolTip = "Drag to swing PSK Charm • Right click for menu"
            };

            var transformGroup = new TransformGroup();
            // Scale and rotate both pivot on the top-center of the charm (where the cord attaches)
            _charmScaleTransform = new ScaleTransform(_settings.CharmScale, _settings.CharmScale, _baseCharmWidth / 2.0, 0);
            _charmRotateTransform = new RotateTransform(0, _baseCharmWidth / 2.0, 0);
            transformGroup.Children.Add(_charmScaleTransform);
            transformGroup.Children.Add(_charmRotateTransform);
            _charmContainer.RenderTransform = transformGroup;

            _charmImage = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };
            _charmImage.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 14,
                Opacity = 0.55,
                ShadowDepth = 5,
                Direction = 270
            };
            _charmContainer.Child = _charmImage;

            // Charm interaction events
            _charmContainer.MouseLeftButtonDown += Charm_MouseDown;
            _charmContainer.MouseMove += Charm_MouseMove;
            _charmContainer.MouseLeftButtonUp += Charm_MouseUp;
            _charmContainer.MouseRightButtonUp += Charm_MouseRightUp;

            _charmContainer.MouseWheel += Charm_MouseWheel;

            _rootCanvas.Children.Add(_charmContainer);

            UpdateCharmAppearance();
            ApplyCordStyle();
        }

        private void InitializePhysics()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double anchorX = screenW * _settings.AnchorPercentX;
            double anchorY = _settings.AnchorOffsetY;

            _simulator = new PhysicsSimulator(anchorX, anchorY, _settings.CordLength);
            UpdateAnchorVisualPosition();
        }

        private void UpdateAnchorFromPercent()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double anchorX = screenW * _settings.AnchorPercentX;
            double anchorY = _settings.AnchorOffsetY;
            if (_simulator != null)
            {
                _simulator.SetAnchor(anchorX, anchorY);
            }
            UpdateAnchorVisualPosition();
        }

        private void UpdateAnchorVisualPosition()
        {
            if (_anchorElement == null || _simulator == null) return;
            Canvas.SetLeft(_anchorElement, _simulator.Anchor.X - (_anchorElement.Width / 2.0));
            Canvas.SetTop(_anchorElement, _simulator.Anchor.Y);
        }

        public void UpdateCharmAppearance()
        {
            var active = _catalog.Charms.Find(c => c.Id == _settings.ActiveCharmId);
            if (active == null && _catalog.Charms.Count > 0)
            {
                active = _catalog.Charms[0];
                _settings.ActiveCharmId = active.Id;
            }

            if (active != null)
            {
                var bmp = active.GetBitmap(350);
                _charmImage.Source = bmp;

                // Pivot/attach on the topmost visible pixel of the artwork, in container coordinates
                if (bmp != null)
                {
                    Point a = active.GetAttachPoint();
                    double s = Math.Min(_baseCharmWidth / bmp.PixelWidth, _baseCharmHeight / bmp.PixelHeight);
                    double w = bmp.PixelWidth * s, h = bmp.PixelHeight * s;
                    _attachX = (_baseCharmWidth - w) / 2.0 + a.X * w;
                    _attachY = a.Y * h + 2.0; // slight overlap so the cord end sits on the artwork
                    _imgHeight = h;

                    Color glowColor = Color.FromRgb(56, 189, 248);
                    try { glowColor = (Color)ColorConverter.ConvertFromString(active.GradientEnd); } catch { }
                    var glowBrush = new RadialGradientBrush();
                    glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(120, glowColor.R, glowColor.G, glowColor.B), 0.0));
                    glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(55, glowColor.R, glowColor.G, glowColor.B), 0.55));
                    glowBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, glowColor.R, glowColor.G, glowColor.B), 1.0));
                    _glow.Fill = glowBrush;
                    _charmScaleTransform.CenterX = _attachX;
                    _charmScaleTransform.CenterY = _attachY;
                    _charmRotateTransform.CenterX = _attachX;
                    _charmRotateTransform.CenterY = _attachY;
                }
                _charmContainer.ToolTip = active.Name + " (" + active.Category + ")\nCreated by PSK Developer\nDrag to swing • Right-click for options";
            }

            _charmScaleTransform.ScaleX = _settings.CharmScale;
            _charmScaleTransform.ScaleY = _settings.CharmScale;
            ApplyCordStyle();
        }

        public void ApplyCordStyle()
        {
            Color cordColor;
            try
            {
                cordColor = (Color)ColorConverter.ConvertFromString(_settings.CordColorHex);
            }
            catch
            {
                cordColor = Color.FromRgb(255, 215, 0); // Gold
            }

            _ropePath.Stroke = new SolidColorBrush(cordColor);
            _ropePath.StrokeThickness = _settings.CordThickness;

            if (_settings.CordStyle == "NeonGlow")
            {
                _ropePath.Effect = new DropShadowEffect
                {
                    Color = cordColor,
                    BlurRadius = 14,
                    Opacity = 0.95,
                    ShadowDepth = 0
                };
            }
            else if (_settings.CordStyle == "GoldenThread")
            {
                _ropePath.Stroke = new LinearGradientBrush(
                    Color.FromRgb(255, 235, 140),
                    Color.FromRgb(180, 130, 20),
                    new Point(0, 0),
                    new Point(1, 1));
                _ropePath.Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(250, 204, 21),
                    BlurRadius = 6,
                    Opacity = 0.7,
                    ShadowDepth = 1
                };
            }
            else
            {
                _ropePath.Effect = null;
            }

            if (_settings.CordStyle == "Invisible")
            {
                _ropePath.Visibility = Visibility.Collapsed;
                foreach (var b in _beads) b.Visibility = Visibility.Collapsed;
            }
            else if (_settings.CordStyle == "BeadChain")
            {
                _ropePath.Visibility = Visibility.Visible;
                _ropePath.StrokeThickness = 1.5;
                // Beads visibility handled in render loop
            }
            else
            {
                _ropePath.Visibility = Visibility.Visible;
                foreach (var b in _beads) b.Visibility = Visibility.Collapsed;
            }

            if (_simulator != null)
            {
                _simulator.SetLength(_settings.CordLength);
            }
        }

        public void ReloadSettings()
        {
            Topmost = _settings.AlwaysOnTop;
            if (_settings.AlwaysOnTop)
            {
                Win32Helper.MakeTopMost(this);
            }
            Win32Helper.SetClickThrough(this, _settings.ClickThrough);
            UpdateAnchorFromPercent();
            UpdateCharmAppearance();
        }

        private void OnRenderingFrame(object sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            double dt = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;

            // Clamp dt to avoid huge step jumps
            if (dt > 0.05) dt = 0.05;
            if (dt < 0.001) dt = 0.001;

            _simulator.Step(
                dt,
                _settings.AmbientBreeze,
                _settings.BreezeIntensity,
                _settings.Gravity,
                _settings.Damping);

            RenderSimulation();
        }

        private void RenderSimulation()
        {
            var points = _simulator.Points;
            int count = PhysicsSimulator.LinkCount;

            // Draw smooth rope path using lightweight frozen StreamGeometry with Catmull-Rom spline
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(points[0].X, points[0].Y), false, false);
                for (int i = 0; i < count - 1; i++)
                {
                    Point p0 = new Point(points[i].X, points[i].Y);
                    Point p1 = new Point(points[i + 1].X, points[i + 1].Y);

                    // Catmull-Rom smooth tangents
                    Point prev = (i == 0)
                        ? new Point(points[0].X, points[0].Y - (points[1].Y - points[0].Y))
                        : new Point(points[i - 1].X, points[i - 1].Y);

                    Point next = (i + 2 < count)
                        ? new Point(points[i + 2].X, points[i + 2].Y)
                        : new Point(points[count - 1].X + (points[count - 1].X - points[count - 2].X),
                                    points[count - 1].Y + (points[count - 1].Y - points[count - 2].Y));

                    Point c1 = new Point(p0.X + (p1.X - prev.X) / 6.0, p0.Y + (p1.Y - prev.Y) / 6.0);
                    Point c2 = new Point(p1.X - (next.X - p0.X) / 6.0, p1.Y - (next.Y - p0.Y) / 6.0);
                    ctx.BezierTo(c1, c2, p1, true, false);
                }
            }
            geometry.Freeze();
            _ropePath.Data = geometry;

            // Render beads if BeadChain style
            if (_settings.CordStyle == "BeadChain")
            {
                Color beadColor;
                try { beadColor = (Color)ColorConverter.ConvertFromString(_settings.CordColorHex); }
                catch { beadColor = Color.FromRgb(250, 204, 21); }

                int beadIdx = 0;
                double beadRadius = 4.5;
                for (int i = 0; i < count - 1 && beadIdx < _beads.Count - 1; i++)
                {
                    Point pA = new Point(points[i].X, points[i].Y);
                    Point pB = new Point(points[i + 1].X, points[i + 1].Y);

                    // 2 beads per link
                    for (int sub = 0; sub < 2 && beadIdx < _beads.Count; sub++)
                    {
                        double t = (sub + 1) / 3.0;
                        double bx = pA.X + (pB.X - pA.X) * t;
                        double by = pA.Y + (pB.Y - pA.Y) * t;

                        var b = _beads[beadIdx++];
                        b.Fill = new SolidColorBrush(beadColor);
                        b.Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        b.StrokeThickness = 0.8;
                        b.Width = beadRadius * 2;
                        b.Height = beadRadius * 2;
                        b.Visibility = Visibility.Visible;
                        Canvas.SetLeft(b, bx - beadRadius);
                        Canvas.SetTop(b, by - beadRadius);
                    }
                }
                while (beadIdx < _beads.Count)
                {
                    _beads[beadIdx++].Visibility = Visibility.Collapsed;
                }
            }

            // Position and rotate charm
            Point charmPos = _simulator.GetCharmPosition();
            double angle = _simulator.GetRotationAngle();

            // Scale/rotate pivot on the attach point, so it stays exactly on the cord end
            double charmLeft = charmPos.X - _attachX;
            double charmTop = charmPos.Y - _attachY;

            Canvas.SetLeft(_charmContainer, charmLeft);
            Canvas.SetTop(_charmContainer, charmTop);

            // Soft glow backdrop follows the middle of the charm (it does not rotate)
            double sc = _settings.CharmScale;
            double gx = charmPos.X + (_baseCharmWidth / 2.0 - _attachX) * sc;
            double gy = charmPos.Y + (_imgHeight / 2.0 - _attachY) * sc;
            Canvas.SetLeft(_glow, gx - _glow.Width / 2.0);
            Canvas.SetTop(_glow, gy - _glow.Height / 2.0);

            _charmRotateTransform.Angle = angle;
        }

        #region Charm Mouse Dragging

        private bool _isDraggingReposition = false;

        private void Charm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(_rootCanvas);

            // Shift-Click, Ctrl-Click, or Middle-Click: Direct Anchor Reposition mode!
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                e.MiddleButton == MouseButtonState.Pressed)
            {
                _isDraggingReposition = true;
                _charmContainer.CaptureMouse();
                _charmContainer.Cursor = Cursors.SizeAll;
                RepositionAnchorToX(pos.X);
                e.Handled = true;
                return;
            }

            // Double-click: switch to the next charm
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (_simulator.IsDragging) _simulator.EndDrag();
                _charmContainer.ReleaseMouseCapture();
                if (RequestNextCharm != null) RequestNextCharm();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left)
            {
                _simulator.BeginDrag(pos.X, pos.Y);
                _charmContainer.CaptureMouse();
                _charmContainer.Cursor = Cursors.Hand;
                e.Handled = true;
            }
        }

        private void Charm_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.GetPosition(_rootCanvas);

            if (_isDraggingReposition)
            {
                RepositionAnchorToX(pos.X);
                e.Handled = true;
                return;
            }

            if (_simulator.IsDragging)
            {
                _simulator.Drag(pos.X, pos.Y);

                // Horizontal glide: if user pulls charm sideways far across the screen,
                // smoothly glide anchor along with them so the charm can be moved anywhere!
                double anchorX = _simulator.Anchor.X;
                double deltaX = pos.X - anchorX;
                double maxPull = Math.Max(120.0, _settings.CordLength * 0.85);

                if (Math.Abs(deltaX) > maxPull)
                {
                    double excess = deltaX > 0 ? (deltaX - maxPull) : (deltaX + maxPull);
                    RepositionAnchorToX(anchorX + excess * 0.22);
                }

                e.Handled = true;
            }
        }

        private void Charm_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingReposition)
            {
                _isDraggingReposition = false;
                _charmContainer.ReleaseMouseCapture();
                _charmContainer.Cursor = Cursors.Hand;
                _settings.Save();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left && _simulator.IsDragging)
            {
                _simulator.EndDrag();
                _charmContainer.ReleaseMouseCapture();
                _charmContainer.Cursor = Cursors.Hand;
                _settings.Save();
                e.Handled = true;
            }
        }

        public void RepositionAnchorToX(double screenX)
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double clampedX = Math.Max(30.0, Math.Min(screenW - 30.0, screenX));
            _settings.AnchorPercentX = clampedX / screenW;
            if (_simulator != null)
            {
                _simulator.SetAnchor(clampedX, _settings.AnchorOffsetY);
            }
            UpdateAnchorVisualPosition();
        }

        public void SetAnchorPercentX(double percent)
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double clamped = Math.Max(0.04, Math.Min(0.96, percent));
            _settings.AnchorPercentX = clamped;
            if (_simulator != null)
            {
                _simulator.SetAnchor(clamped * screenW, _settings.AnchorOffsetY);
            }
            UpdateAnchorVisualPosition();
            _settings.Save();
        }

        public void TriggerActionImpulse(string actionType)
        {
            if (_simulator == null) return;
            string act = (actionType ?? "").ToLowerInvariant();
            if (act.Contains("spin"))
            {
                _simulator.ApplyImpulse(95.0, -25.0);
            }
            else if (act.Contains("rub") || act.Contains("luck") || act.Contains("sway"))
            {
                _simulator.ApplyImpulse(55.0, -12.0);
            }
            else if (act.Contains("flip") || act.Contains("turn"))
            {
                _simulator.ApplyImpulse(-75.0, -35.0);
            }
            else if (act.Contains("ward") || act.Contains("evil"))
            {
                _simulator.ApplyImpulse(110.0, -15.0);
            }
            else
            {
                _simulator.ApplyImpulse(65.0, -25.0);
            }
        }

        private void Charm_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double step = e.Delta > 0 ? 0.05 : -0.05;
            _settings.CharmScale = Math.Max(0.5, Math.Min(2.0, _settings.CharmScale + step));
            _settings.Save();
            UpdateCharmAppearance();
            e.Handled = true;
        }

        private void Charm_MouseRightUp(object sender, MouseButtonEventArgs e)
        {
            ShowCharmContextMenu();
            e.Handled = true;
        }

        private void ShowCharmContextMenu()
        {
            var menu = new ContextMenu();
            menu.Style = CreateContextMenuStyle();

            var miTitle = new MenuItem
            {
                Header = "PSK Charms • PSK Developer",
                IsEnabled = false,
                FontWeight = FontWeights.Bold
            };
            menu.Items.Add(miTitle);
            menu.Items.Add(new Separator());

            var miSettings = new MenuItem { Header = "✨ Open Control Center & Sticker Studio" };
            miSettings.Click += (s, ev) => { if (RequestOpenControlCenter != null) RequestOpenControlCenter(); };
            menu.Items.Add(miSettings);

            var miNext = new MenuItem { Header = "🔀 Next Charm" };
            miNext.Click += (s, ev) => { if (RequestNextCharm != null) RequestNextCharm(); };
            menu.Items.Add(miNext);

            menu.Items.Add(new Separator());

            var miSnapLeft = new MenuItem { Header = "◂ Snap Charm to Left (20%)" };
            miSnapLeft.Click += (s, ev) => SetAnchorPercentX(0.20);
            menu.Items.Add(miSnapLeft);

            var miReset = new MenuItem { Header = "📍 Snap Charm to Center (50%)" };
            miReset.Click += (s, ev) => SetAnchorPercentX(0.50);
            menu.Items.Add(miReset);

            var miSnapRight = new MenuItem { Header = "▸ Snap Charm to Right (80%)" };
            miSnapRight.Click += (s, ev) => SetAnchorPercentX(0.80);
            menu.Items.Add(miSnapRight);

            menu.Items.Add(new Separator());

            var miBreeze = new MenuItem
            {
                Header = "🌬️ Idle Breeze Swaying",
                IsCheckable = true,
                IsChecked = _settings.AmbientBreeze
            };
            miBreeze.Click += (s, ev) =>
            {
                _settings.AmbientBreeze = miBreeze.IsChecked;
                _settings.Save();
            };
            menu.Items.Add(miBreeze);

            var miTop = new MenuItem
            {
                Header = "📌 Always On Top",
                IsCheckable = true,
                IsChecked = _settings.AlwaysOnTop
            };
            miTop.Click += (s, ev) =>
            {
                _settings.AlwaysOnTop = miTop.IsChecked;
                _settings.Save();
                ReloadSettings();
            };
            menu.Items.Add(miTop);

            var miGlow = new MenuItem
            {
                Header = "💫 Background Glow",
                IsCheckable = true,
                IsChecked = _settings.GlowBackdrop
            };
            miGlow.Click += (s, ev) =>
            {
                _settings.GlowBackdrop = miGlow.IsChecked;
                _settings.Save();
                _glow.Visibility = _settings.GlowBackdrop ? Visibility.Visible : Visibility.Collapsed;
            };
            menu.Items.Add(miGlow);

            var miShot =new MenuItem { Header = "📸 Screenshot Mode (20s)" };
            miShot.Click += (s, ev) => EnterScreenshotMode(20);
            menu.Items.Add(miShot);

            menu.Items.Add(new Separator());

            var miExit =new MenuItem { Header = "❌ Exit PSK" };
            miExit.Click += (s, ev) => { if (RequestExit != null) RequestExit(); };
            menu.Items.Add(miExit);

            menu.IsOpen = true;
        }

        private Style CreateContextMenuStyle()
        {
            var style = new Style(typeof(ContextMenu));
            style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(24, 26, 38))));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(230, 230, 235))));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(60, 64, 90))));
            style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 13.0));
            return style;
        }

        #endregion

        #region Anchor Dragging

        private void Anchor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDraggingAnchor = true;
                _anchorDragStartX = e.GetPosition(_rootCanvas).X;
                _anchorElement.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Anchor_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingAnchor)
            {
                Point pos = e.GetPosition(_rootCanvas);
                double screenW = SystemParameters.PrimaryScreenWidth;
                double clampedX = Math.Max(20.0, Math.Min(screenW - 20.0, pos.X));

                _settings.AnchorPercentX = clampedX / screenW;
                _simulator.SetAnchor(clampedX, _settings.AnchorOffsetY);
                UpdateAnchorVisualPosition();
                e.Handled = true;
            }
        }

        private void Anchor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isDraggingAnchor)
            {
                _isDraggingAnchor = false;
                _anchorElement.ReleaseMouseCapture();
                _settings.Save();
                e.Handled = true;
            }
        }

        #endregion
    }
}
