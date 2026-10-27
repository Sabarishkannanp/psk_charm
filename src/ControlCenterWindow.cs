using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace PSKCharms
{
    public class ControlCenterWindow : Window
    {
        private AppSettings _settings;
        private CharmCatalog _catalog;
        private OverlayWindow _overlayWindow;

        // Navigation
        private Grid _mainGrid;
        private ContentControl _tabContentArea;
        private List<Border> _tabButtons = new List<Border>();
        private int _currentTabIndex = 0;

        // Catalog & Cards
        private StackPanel _cardsStackPanel;
        private TextBox _searchBox;
        private string _selectedCategory = "All";
        private List<Border> _categoryChips = new List<Border>();
        private StackPanel _categoryChipsPanel;

        // Customizer Sliders (Top card)
        private Slider _charmScaleSlider;
        private TextBlock _charmScaleLabel;
        private Slider _cordLengthSlider;
        private TextBlock _cordLengthLabel;
        private Slider _cordThicknessSlider;
        private TextBlock _cordThicknessLabel;
        private Slider _positionSlider;
        private TextBlock _positionLabel;

        // Sticker Studio Elements (Bottom card)
        private Image _stickerPreviewImage;
        private Border _checkerboardBox;
        private TextBox _stickerTagBox;
        private Border _cutoutToggleTrack;
        private Border _cutoutToggleThumb;
        private bool _isCutoutEnabled = true;
        private Slider _outlineSlider;
        private TextBlock _outlineLabel;
        private Slider _shadowSlider;
        private TextBlock _shadowLabel;

        // Colors
        private readonly string[] _presetColors = new string[]
        {
            "#EF4444", // Red
            "#F97316", // Coral Orange
            "#F59E0B", // Sunburst Yellow
            "#10B981", // Emerald Green
            "#06B6D4", // Neon Cyan
            "#3B82F6", // Royal Blue
            "#8B5CF6", // Cyber Purple
            "#EC4899", // Hot Pink
            "#F8FAFC"  // Pure White
        };

        public ControlCenterWindow(AppSettings settings, CharmCatalog catalog, OverlayWindow overlay)
        {
            _settings = settings;
            _catalog = catalog;
            _overlayWindow = overlay;

            InitializeWindow();
            BuildUI();
            SwitchTab(0);
        }

        private void InitializeWindow()
        {
            Title = "PSK Charms • Created by PSK Developer";
            Width = 1060;
            Height = 740;
            MinWidth = 920;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(Color.FromRgb(10, 14, 23)); // #0A0E17
            Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 245));
            FontFamily = new FontFamily("Segoe UI, Inter, Arial");

            try
            {
                string iconPath = Path.Combine(CharmItem.GetAssetsRoot(), "Assets", "psk.ico");
                if (File.Exists(iconPath))
                {
                    Icon = BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                }
            }
            catch { }

            Closing += (s, e) =>
            {
                e.Cancel = true;
                Hide();
            };
        }

        private void BuildUI()
        {
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(68) }); // Header
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content

            BuildHeader();

            _tabContentArea = new ContentControl();
            Grid.SetRow(_tabContentArea, 1);
            _mainGrid.Children.Add(_tabContentArea);

            Content = _mainGrid;
        }

        #region Header

        private void BuildHeader()
        {
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 18, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(26, 33, 52)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 0, 20, 0)
            };
            Grid.SetRow(headerBorder, 0);

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Brand & Avatar
            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var avatarBorder = new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(22),
                Background = new SolidColorBrush(Color.FromRgb(20, 26, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)), // Cyan glow
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 14, 0),
                ClipToBounds = true
            };
            avatarBorder.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(56, 189, 248),
                BlurRadius = 10,
                Opacity = 0.5,
                ShadowDepth = 0
            };

            string avatarPath = Path.Combine(CharmItem.GetAssetsRoot(), "Assets", "psk_developer.png");
            if (File.Exists(avatarPath))
            {
                avatarBorder.Child = new Image
                {
                    Source = new BitmapImage(new Uri(avatarPath, UriKind.Absolute)),
                    Stretch = Stretch.UniformToFill,
                    ToolTip = "PSK Developer • Lead Creator"
                };
            }
            brandStack.Children.Add(avatarBorder);

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleText = new TextBlock
            {
                Text = "PSK Charms",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            var subText = new TextBlock
            {
                Text = "Created by PSK Developer",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)) // Cyan accent
            };
            titleStack.Children.Add(titleText);
            titleStack.Children.Add(subText);
            brandStack.Children.Add(titleStack);

            Grid.SetColumn(brandStack, 0);
            headerGrid.Children.Add(brandStack);

            // Nav Tabs
            var tabsStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            _tabButtons.Clear();
            _tabButtons.Add(CreateNavTab("✨ Editor", 0));
            _tabButtons.Add(CreateNavTab("🎛️ Mixer", 1));
            _tabButtons.Add(CreateNavTab("👤 Creator Profile", 2));
            _tabButtons.Add(CreateNavTab("⚙️ Settings", 3));

            foreach (var tb in _tabButtons)
            {
                tabsStack.Children.Add(tb);
            }

            Grid.SetColumn(tabsStack, 1);
            headerGrid.Children.Add(tabsStack);

            // Right banner: "Turn your photo into a sticker! ✨"
            var rightStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            var bannerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 6, 182, 212)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 6, 182, 212)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 14, 0)
            };
            var bannerText = new TextBlock
            {
                Text = "Turn your photo into a sticker! ✨",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(125, 211, 252))
            };
            bannerBorder.Child = bannerText;
            rightStack.Children.Add(bannerBorder);

            var minBtn = new Button
            {
                Content = "—",
                Width = 32,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(24, 30, 46)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 13,
                Margin = new Thickness(0, 0, 4, 0)
            };
            minBtn.Click += (s, e) => Hide();
            rightStack.Children.Add(minBtn);

            Grid.SetColumn(rightStack, 2);
            headerGrid.Children.Add(rightStack);

            headerBorder.Child = headerGrid;
            _mainGrid.Children.Add(headerBorder);
        }

        private Border CreateNavTab(string text, int index)
        {
            var btn = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14, 6, 14, 6),
                Margin = new Thickness(4, 0, 4, 0),
                Cursor = Cursors.Hand,
                Background = Brushes.Transparent
            };
            var txt = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 185))
            };
            btn.Child = txt;
            btn.MouseLeftButtonDown += (s, e) => SwitchTab(index);
            return btn;
        }

        private void SwitchTab(int index)
        {
            _currentTabIndex = index;
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var tb = _tabButtons[i];
                var txt = tb.Child as TextBlock;
                if (i == index)
                {
                    tb.Background = new SolidColorBrush(Color.FromRgb(30, 41, 65));
                    tb.BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                    tb.BorderThickness = new Thickness(1);
                    if (txt != null)
                    {
                        txt.Foreground = Brushes.White;
                        txt.FontWeight = FontWeights.Bold;
                    }
                }
                else
                {
                    tb.Background = Brushes.Transparent;
                    tb.BorderThickness = new Thickness(0);
                    if (txt != null)
                    {
                        txt.Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 185));
                        txt.FontWeight = FontWeights.Medium;
                    }
                }
            }

            switch (index)
            {
                case 0: _tabContentArea.Content = BuildEditorView(); break;
                case 1: _tabContentArea.Content = BuildMixerView(); break;
                case 2: _tabContentArea.Content = BuildProfileView(); break;
                case 3: _tabContentArea.Content = BuildSettingsView(); break;
            }
        }

        #endregion

        #region Tab 0: Modern Editor & Card Studio (Matching Attached Photo)

        private UIElement BuildEditorView()
        {
            var mainGrid = new Grid { Margin = new Thickness(18, 14, 18, 14) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(460) }); // Left Pane: Charms
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });  // Gap
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Right Pane: Studio

            // ================= LEFT COLUMN =================
            var leftPane = new Grid();
            leftPane.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search
            leftPane.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Categories
            leftPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Cards List

            // Search bar
            var searchBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 24, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(32, 42, 66)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var searchIcon = new TextBlock
            {
                Text = "🔍 ",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 130, 160)),
                VerticalAlignment = VerticalAlignment.Center
            };
            _searchBox = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            _searchBox.TextChanged += (s, e) => FilterAndRenderCards();

            Grid.SetColumn(searchIcon, 0);
            Grid.SetColumn(_searchBox, 1);
            searchGrid.Children.Add(searchIcon);
            searchGrid.Children.Add(_searchBox);
            searchBorder.Child = searchGrid;

            Grid.SetRow(searchBorder, 0);
            leftPane.Children.Add(searchBorder);

            // Category filter chips
            var chipsScroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _categoryChipsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            _categoryChips.Clear();

            foreach (var cat in _catalog.Categories)
            {
                var chip = CreateCategoryChip(cat);
                _categoryChips.Add(chip);
                _categoryChipsPanel.Children.Add(chip);
            }
            chipsScroll.Content = _categoryChipsPanel;
            Grid.SetRow(chipsScroll, 1);
            leftPane.Children.Add(chipsScroll);

            // Cards ScrollViewer
            var cardsScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            _cardsStackPanel = new StackPanel();
            cardsScroll.Content = _cardsStackPanel;
            Grid.SetRow(cardsScroll, 2);
            leftPane.Children.Add(cardsScroll);

            Grid.SetColumn(leftPane, 0);
            mainGrid.Children.Add(leftPane);

            // ================= RIGHT COLUMN =================
            var rightPane = new Grid();
            rightPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(250) }); // Top: Customize your sticker
            rightPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });  // Gap
            rightPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Bottom: Make a sticker
            rightPane.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });  // Footer note

            // Top Card: Customize your sticker
            var customCard = BuildCustomizeStickerCard();
            Grid.SetRow(customCard, 0);
            rightPane.Children.Add(customCard);

            // Bottom Card: Make a sticker
            var makeCard = BuildMakeStickerCard();
            Grid.SetRow(makeCard, 2);
            rightPane.Children.Add(makeCard);

            // Bottom note
            var bottomNote = new TextBlock
            {
                Text = "Collect them all! -> ❤️",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 244, 114, 182)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 6, 6, 0)
            };
            Grid.SetRow(bottomNote, 3);
            rightPane.Children.Add(bottomNote);

            Grid.SetColumn(rightPane, 2);
            mainGrid.Children.Add(rightPane);

            FilterAndRenderCards();
            UpdateStickerStudioPreview();

            return mainGrid;
        }

        private Border CreateCategoryChip(string categoryName)
        {
            bool isSelected = categoryName == _selectedCategory;
            var chip = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 5, 12, 5),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                Background = isSelected ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(20, 26, 42)),
                BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(125, 211, 252)) : new SolidColorBrush(Color.FromRgb(36, 44, 70)),
                BorderThickness = new Thickness(1)
            };
            var txt = new TextBlock
            {
                Text = categoryName,
                FontSize = 12,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isSelected ? Brushes.Black : new SolidColorBrush(Color.FromRgb(210, 215, 235))
            };
            chip.Child = txt;
            chip.MouseLeftButtonDown += (s, e) =>
            {
                _selectedCategory = categoryName;
                UpdateCategoryChipsHighlight();
                FilterAndRenderCards();
            };
            return chip;
        }

        private void UpdateCategoryChipsHighlight()
        {
            for (int i = 0; i < _categoryChips.Count; i++)
            {
                var chip = _categoryChips[i];
                var txt = chip.Child as TextBlock;
                if (txt != null)
                {
                    bool isSel = (txt.Text == _selectedCategory);
                    chip.Background = isSel ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(20, 26, 42));
                    chip.BorderBrush = isSel ? new SolidColorBrush(Color.FromRgb(125, 211, 252)) : new SolidColorBrush(Color.FromRgb(36, 44, 70));
                    txt.Foreground = isSel ? Brushes.Black : new SolidColorBrush(Color.FromRgb(210, 215, 235));
                    txt.FontWeight = isSel ? FontWeights.Bold : FontWeights.Normal;
                }
            }
        }

        private void FilterAndRenderCards()
        {
            if (_cardsStackPanel == null) return;
            _cardsStackPanel.Children.Clear();

            string query = (_searchBox != null ? _searchBox.Text.Trim().ToLowerInvariant() : "");

            var filtered = _catalog.Charms.Where(c =>
            {
                if (_selectedCategory != "All" && c.Category != _selectedCategory)
                    return false;
                if (!string.IsNullOrEmpty(query) &&
                    !c.Name.ToLowerInvariant().Contains(query) &&
                    !c.Category.ToLowerInvariant().Contains(query) &&
                    !(c.Origin ?? "").ToLowerInvariant().Contains(query))
                    return false;
                return true;
            }).ToList();

            foreach (var charm in filtered)
            {
                var card = CreateModernCharmCard(charm);
                _cardsStackPanel.Children.Add(card);
            }
        }

        private UIElement CreateModernCharmCard(CharmItem charm)
        {
            bool isActive = (charm.Id == _settings.ActiveCharmId);

            Color gStart, gEnd;
            try { gStart = (Color)ColorConverter.ConvertFromString(charm.GradientStart); }
            catch { gStart = Color.FromRgb(25, 33, 52); }

            try { gEnd = (Color)ColorConverter.ConvertFromString(charm.GradientEnd); }
            catch { gEnd = Color.FromRgb(15, 20, 32); }

            var card = new Border
            {
                Height = 100,
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(14),
                Background = new LinearGradientBrush(gStart, gEnd, new Point(0, 0), new Point(1, 1)),
                BorderBrush = isActive ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(isActive ? 2.2 : 1),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand
            };

            card.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = isActive ? 12 : 6,
                Opacity = isActive ? 0.6 : 0.35,
                ShadowDepth = 3
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) }); // Thumbnail
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action button

            // Charm Thumbnail
            var thumbBorder = new Border
            {
                Width = 64,
                Height = 74,
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var img = new Image
            {
                Source = charm.GetBitmap(140),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            img.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 8,
                Opacity = 0.5,
                ShadowDepth = 2
            };
            thumbBorder.Child = img;
            Grid.SetColumn(thumbBorder, 0);
            grid.Children.Add(thumbBorder);

            // Info Stack
            var infoStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 8, 0)
            };

            var nameText = new TextBlock
            {
                Text = charm.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var originText = new TextBlock
            {
                Text = (charm.Origin ?? charm.Category).ToUpperInvariant(),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(186, 230, 253)),
                Margin = new Thickness(0, 1, 0, 3)
            };
            var descText = new TextBlock
            {
                Text = charm.Description,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromArgb(220, 240, 245, 255)),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 32,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            infoStack.Children.Add(nameText);
            infoStack.Children.Add(originText);
            infoStack.Children.Add(descText);
            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            // Action Pill Button (e.g. "🔄 Spin it once", "🍀 Rub for luck", "✨ Good vibes")
            var actBtn = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 6, 10, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            var actTxt = new TextBlock
            {
                Text = !string.IsNullOrEmpty(charm.ActionText) ? charm.ActionText : "✨ Swing",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };
            actBtn.Child = actTxt;

            actBtn.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                SelectCharm(charm);
                _overlayWindow.TriggerActionImpulse(charm.ActionText);
            };

            actBtn.MouseEnter += (s, e) =>
            {
                actBtn.Background = new SolidColorBrush(Color.FromArgb(130, 255, 255, 255));
                actTxt.Foreground = Brushes.Black;
            };
            actBtn.MouseLeave += (s, e) =>
            {
                actBtn.Background = new SolidColorBrush(Color.FromArgb(70, 0, 0, 0));
                actTxt.Foreground = Brushes.White;
            };

            Grid.SetColumn(actBtn, 2);
            grid.Children.Add(actBtn);

            card.Child = grid;

            // Click card to select
            card.MouseLeftButtonDown += (s, e) =>
            {
                SelectCharm(charm);
            };

            return card;
        }

        private void SelectCharm(CharmItem charm)
        {
            _settings.ActiveCharmId = charm.Id;
            _settings.ActiveCharmName = charm.Name;
            _settings.ActiveCharmPath = charm.RelativePath;
            _settings.Save();

            _overlayWindow.UpdateCharmAppearance();
            FilterAndRenderCards();
            UpdateStickerStudioPreview();
        }

        public void SelectNextCharm()
        {
            if (_catalog.Charms.Count == 0) return;
            int idx = _catalog.Charms.FindIndex(c => c.Id == _settings.ActiveCharmId);
            idx = (idx + 1) % _catalog.Charms.Count;
            SelectCharm(_catalog.Charms[idx]);
        }

        #endregion

        #region Right Pane Cards: Customize your sticker & Make a sticker

        private Border BuildCustomizeStickerCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 22, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 38, 58)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16, 12, 16, 12)
            };

            var stack = new StackPanel();

            // Header
            var headGrid = new Grid();
            headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var hText = new TextBlock
            {
                Text = "🎛️ Customize your sticker",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };
            var sText = new TextBlock
            {
                Text = "Adjust the look to make it truly yours!",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 180)),
                Margin = new Thickness(0, 2, 0, 8)
            };
            stack.Children.Add(hText);
            stack.Children.Add(sText);

            // Sliders Stack
            // 1. Size Slider
            stack.Children.Add(CreateSliderRow("⤢ Size", 0.5, 2.0, _settings.CharmScale, out _charmScaleSlider, out _charmScaleLabel, v =>
            {
                _settings.CharmScale = v;
                _settings.Save();
                _overlayWindow.UpdateCharmAppearance();
                UpdateStickerStudioPreview();
            }, v => Math.Round(v * 100) + "%"));

            // 2. Length Slider
            stack.Children.Add(CreateSliderRow("↕ Length", 80, 450, _settings.CordLength, out _cordLengthSlider, out _cordLengthLabel, v =>
            {
                _settings.CordLength = v;
                _settings.Save();
                _overlayWindow.ApplyCordStyle();
            }, v => Math.Round(v) + "px"));

            // 3. Cord Thickness / Color intensity
            stack.Children.Add(CreateSliderRow("🎨 Cord", 1.5, 7.0, _settings.CordThickness, out _cordThicknessSlider, out _cordThicknessLabel, v =>
            {
                _settings.CordThickness = v;
                _settings.Save();
                _overlayWindow.ApplyCordStyle();
            }, v => Math.Round((v / 7.0) * 100) + "%"));

            // 4. Horizontal Screen Position Slider
            var posRow = CreateSliderRow("📍 Position", 0.05, 0.95, _settings.AnchorPercentX, out _positionSlider, out _positionLabel, v =>
            {
                _overlayWindow.SetAnchorPercentX(v);
            }, v => v < 0.35 ? "Left (" + Math.Round(v * 100) + "%)" : (v > 0.65 ? "Right (" + Math.Round(v * 100) + "%)" : "Center (" + Math.Round(v * 100) + "%)"));
            stack.Children.Add(posRow);

            // Preset Color Palette Dots
            var colorPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var colorLabel = new TextBlock
            {
                Text = "Palette: ",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 190)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            colorPanel.Children.Add(colorLabel);

            foreach (var hex in _presetColors)
            {
                colorPanel.Children.Add(CreateColorDot(hex));
            }

            stack.Children.Add(colorPanel);
            card.Child = stack;
            return card;
        }

        private Border CreateColorDot(string hex)
        {
            Color col = (Color)ColorConverter.ConvertFromString(hex);
            bool isCurrent = string.Equals(_settings.CordColorHex, hex, StringComparison.OrdinalIgnoreCase);

            var dot = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(col),
                BorderBrush = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                BorderThickness = new Thickness(isCurrent ? 2 : 1),
                Margin = new Thickness(0, 0, 7, 0),
                Cursor = Cursors.Hand,
                ToolTip = hex
            };

            dot.MouseLeftButtonDown += (s, e) =>
            {
                _settings.CordColorHex = hex;
                _settings.Save();
                _overlayWindow.ApplyCordStyle();
                // Refresh Editor view sliders/colors
                SwitchTab(0);
            };

            return dot;
        }

        private Grid CreateSliderRow(string label, double min, double max, double val, out Slider slider, out TextBlock valLabel, Action<double> onValChanged, Func<double, string> formatVal)
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });

            var lbl = new TextBlock
            {
                Text = label,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 210, 235)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            var sld = new Slider
            {
                Minimum = min,
                Maximum = max,
                Value = val,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0)
            };
            slider = sld;
            Grid.SetColumn(sld, 1);
            grid.Children.Add(sld);

            var vLbl = new TextBlock
            {
                Text = formatVal(val),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            valLabel = vLbl;
            Grid.SetColumn(vLbl, 2);
            grid.Children.Add(vLbl);

            sld.ValueChanged += (s, e) =>
            {
                vLbl.Text = formatVal(sld.Value);
                onValChanged(sld.Value);
            };

            return grid;
        }

        private Border BuildMakeStickerCard()
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 22, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 38, 58)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16, 12, 16, 12)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Studio Layout

            // Header
            var hText = new TextBlock
            {
                Text = "🖼️ Make a sticker",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(hText, 0);
            mainGrid.Children.Add(hText);

            // Studio Layout: Left Preview Box, Right Controls
            var studioGrid = new Grid();
            studioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Checkerboard box
            studioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });  // Gap
            studioGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Controls

            // 1. Checkerboard Preview Box
            _checkerboardBox = new Border
            {
                Width = 200,
                Height = 220,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 52, 80)),
                BorderThickness = new Thickness(1.5),
                Background = CreateCheckerboardBrush(),
                AllowDrop = true
            };

            // Support drag and drop image directly onto preview!
            _checkerboardBox.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0)
                    {
                        ImportUserPhoto(files[0]);
                    }
                }
            };

            var previewCanvas = new Grid();

            // Crown Badge in top left
            var crownBadge = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(10, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(160, 10, 14, 25)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 3, 6, 3)
            };
            var crownTxt = new TextBlock
            {
                Text = "👑 PSK",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21))
            };
            crownBadge.Child = crownTxt;
            previewCanvas.Children.Add(crownBadge);

            // Centered Sticker Image
            _stickerPreviewImage = new Image
            {
                Width = 140,
                Height = 140,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _stickerPreviewImage.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 14,
                Opacity = 0.5,
                ShadowDepth = 4
            };
            previewCanvas.Children.Add(_stickerPreviewImage);

            // Bottom Sticker Tag (e.g. "Good Vibes")
            var tagBorder = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 10),
                Background = new SolidColorBrush(Color.FromArgb(190, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 3, 12, 3)
            };
            _stickerTagBox = new TextBox
            {
                Text = "Good Vibes ✨",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 230, 255)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };
            tagBorder.Child = _stickerTagBox;
            previewCanvas.Children.Add(tagBorder);

            _checkerboardBox.Child = previewCanvas;
            Grid.SetColumn(_checkerboardBox, 0);
            studioGrid.Children.Add(_checkerboardBox);

            // 2. Right Controls
            var ctrlStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            // Cutout Toggle
            var toggleRow = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            toggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            toggleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var cutoutLbl = new TextBlock
            {
                Text = "✂️ Cutout",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(cutoutLbl, 0);
            toggleRow.Children.Add(cutoutLbl);

            var toggleBtn = CreateModernToggleSwitch(out _cutoutToggleTrack, out _cutoutToggleThumb, () =>
            {
                _isCutoutEnabled = !_isCutoutEnabled;
                UpdateToggleVisual();
                UpdateStickerStudioPreview();
            });
            Grid.SetColumn(toggleBtn, 1);
            toggleRow.Children.Add(toggleBtn);
            ctrlStack.Children.Add(toggleRow);

            // Outline Slider
            ctrlStack.Children.Add(CreateSliderRow("⭕ Outline", 0, 100, 0, out _outlineSlider, out _outlineLabel, v =>
            {
                // updates outline preview
            }, v => Math.Round(v) + "%"));

            // Shadow Slider
            ctrlStack.Children.Add(CreateSliderRow("👥 Shadow", 0, 100, 40, out _shadowSlider, out _shadowLabel, v =>
            {
                if (_stickerPreviewImage != null)
                {
                    var drop = _stickerPreviewImage.Effect as DropShadowEffect;
                    if (drop != null)
                    {
                        drop.Opacity = v / 100.0;
                        drop.BlurRadius = (v / 100.0) * 20.0;
                    }
                }
            }, v => Math.Round(v) + "%"));

            // Action Buttons
            // [ ◀ Hang ] Button (Cyan/Blue gradient pill)
            var hangBtn = new Button
            {
                Height = 38,
                Margin = new Thickness(0, 12, 0, 8),
                Background = new LinearGradientBrush(
                    Color.FromRgb(6, 182, 212), // Cyan
                    Color.FromRgb(37, 99, 235), // Royal Blue
                    new Point(0, 0),
                    new Point(1, 0)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Content = "◀  Hang"
            };
            hangBtn.Click += (s, e) =>
            {
                // Hang active charm and trigger pulse
                _overlayWindow.UpdateCharmAppearance();
                _overlayWindow.TriggerActionImpulse("bounce");
            };
            ctrlStack.Children.Add(hangBtn);

            // Secondary Buttons Grid: Upload Image & Save PNG
            var secGrid = new Grid();
            secGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            secGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            secGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var uploadBtn = new Button
            {
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 65)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Content = "📁 Upload Image"
            };
            uploadBtn.Click += (s, e) => PromptUploadImage();
            Grid.SetColumn(uploadBtn, 0);
            secGrid.Children.Add(uploadBtn);

            var saveBtn = new Button
            {
                Height = 34,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 65)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(70, 80, 110)),
                BorderThickness = new Thickness(1),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Content = "💾 Save PNG"
            };
            saveBtn.Click += (s, e) => SaveStickerAsPng();
            Grid.SetColumn(saveBtn, 2);
            secGrid.Children.Add(saveBtn);

            ctrlStack.Children.Add(secGrid);

            Grid.SetColumn(ctrlStack, 2);
            studioGrid.Children.Add(ctrlStack);

            Grid.SetRow(studioGrid, 1);
            mainGrid.Children.Add(studioGrid);

            card.Child = mainGrid;
            return card;
        }

        private UIElement CreateModernToggleSwitch(out Border track, out Border thumb, Action onToggle)
        {
            track = new Border
            {
                Width = 44,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Cursor = Cursors.Hand,
                Padding = new Thickness(2)
            };

            thumb = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            track.Child = thumb;
            track.MouseLeftButtonDown += (s, e) =>
            {
                onToggle();
                e.Handled = true;
            };

            return track;
        }

        private void UpdateToggleVisual()
        {
            if (_cutoutToggleTrack == null || _cutoutToggleThumb == null) return;
            if (_isCutoutEnabled)
            {
                _cutoutToggleTrack.Background = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                _cutoutToggleThumb.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                _cutoutToggleTrack.Background = new SolidColorBrush(Color.FromRgb(60, 70, 95));
                _cutoutToggleThumb.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        private Brush CreateCheckerboardBrush()
        {
            var dg = new DrawingGroup();
            dg.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(22, 28, 44)), null,
                new RectangleGeometry(new Rect(0, 0, 16, 16))));
            dg.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(16, 20, 32)), null,
                new RectangleGeometry(new Rect(0, 0, 8, 8))));
            dg.Children.Add(new GeometryDrawing(
                new SolidColorBrush(Color.FromRgb(16, 20, 32)), null,
                new RectangleGeometry(new Rect(8, 8, 8, 8))));

            var brush = new DrawingBrush(dg)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 16, 16),
                ViewportUnits = BrushMappingMode.Absolute
            };
            brush.Freeze();
            return brush;
        }

        private void UpdateStickerStudioPreview()
        {
            if (_stickerPreviewImage == null) return;

            var active = _catalog.Charms.Find(c => c.Id == _settings.ActiveCharmId);
            if (active == null && _catalog.Charms.Count > 0)
                active = _catalog.Charms[0];

            if (active != null)
            {
                _stickerPreviewImage.Source = active.GetBitmap(240);
                if (_stickerTagBox != null)
                {
                    _stickerTagBox.Text = active.Name;
                }
            }
        }

        private void PromptUploadImage()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Choose Image for Custom PSK Charm",
                Filter = "All Supported Images|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.svg;*.gif|PNG Images|*.png|JPEG Images|*.jpg;*.jpeg|WebP Images|*.webp"
            };

            if (dlg.ShowDialog() == true)
            {
                ImportUserPhoto(dlg.FileName);
            }
        }

        private void ImportUserPhoto(string sourcePath)
        {
            try
            {
                var item = _catalog.AddCustomCharm(sourcePath);
                if (item != null)
                {
                    SelectCharm(item);
                    _selectedCategory = "Custom";
                    UpdateCategoryChipsHighlight();
                    FilterAndRenderCards();
                    MessageBox.Show("Charm '" + item.Name + "' imported and hung successfully!", "PSK Charms", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not import image: " + ex.Message, "PSK Charms", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveStickerAsPng()
        {
            var active = _catalog.Charms.Find(c => c.Id == _settings.ActiveCharmId);
            if (active == null) return;

            var dlg = new SaveFileDialog
            {
                Title = "Save Charm Sticker PNG",
                Filter = "PNG Image|*.png",
                FileName = active.Name + "_Sticker.png"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var bmp = active.GetBitmap(400);
                    if (bmp != null)
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));
                        using (var stream = File.Create(dlg.FileName))
                        {
                            encoder.Save(stream);
                        }
                        MessageBox.Show("Saved sticker successfully to:\n" + dlg.FileName, "PSK Charms", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving sticker: " + ex.Message, "PSK Charms", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Tab 1: Physics & Cord Mixer

        private UIElement BuildMixerView()
        {
            var scroll = new ScrollViewer { Margin = new Thickness(25), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { MaxWidth = 650, HorizontalAlignment = HorizontalAlignment.Left };

            var title = new TextBlock
            {
                Text = "🎛️ Cord Material & Physics Simulation",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            };
            var sub = new TextBlock
            {
                Text = "Fine-tune gravity, rope dynamics, breeze intensity, and visual styles.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 160, 190)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(title);
            stack.Children.Add(sub);

            // Cord Style selector
            var styleLabel = new TextBlock { Text = "Rope Material Style:", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 6) };
            stack.Children.Add(styleLabel);

            var combo = new ComboBox
            {
                Height = 36,
                Margin = new Thickness(0, 0, 0, 20),
                Background = new SolidColorBrush(Color.FromRgb(24, 30, 48)),
                Foreground = Brushes.Black
            };
            combo.Items.Add(new ComboBoxItem { Content = "Classic Rope / Cord", Tag = "Classic" });
            combo.Items.Add(new ComboBoxItem { Content = "Bead Chain (Golden Pearls)", Tag = "BeadChain" });
            combo.Items.Add(new ComboBoxItem { Content = "Golden Thread (Metallic Sheen)", Tag = "GoldenThread" });
            combo.Items.Add(new ComboBoxItem { Content = "Neon Glow (Cyberpunk Beam)", Tag = "NeonGlow" });
            combo.Items.Add(new ComboBoxItem { Content = "Invisible (Floating Charm)", Tag = "Invisible" });

            for (int i = 0; i < combo.Items.Count; i++)
            {
                var it = combo.Items[i] as ComboBoxItem;
                if ((string)it.Tag == _settings.CordStyle)
                {
                    combo.SelectedIndex = i;
                    break;
                }
            }
            if (combo.SelectedIndex < 0) combo.SelectedIndex = 0;

            combo.SelectionChanged += (s, e) =>
            {
                var it = combo.SelectedItem as ComboBoxItem;
                if (it != null)
                {
                    _settings.CordStyle = (string)it.Tag;
                    _settings.Save();
                    _overlayWindow.ApplyCordStyle();
                }
            };
            stack.Children.Add(combo);

            // Ambient Breeze Checkbox
            var breezeCheck = new CheckBox
            {
                Content = "Enable Natural Ambient Breeze (gentle idle swaying)",
                IsChecked = _settings.AmbientBreeze,
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };
            breezeCheck.Click += (s, e) =>
            {
                _settings.AmbientBreeze = (breezeCheck.IsChecked == true);
                _settings.Save();
            };
            stack.Children.Add(breezeCheck);

            // Breeze Intensity
            Slider brSlider; TextBlock brLbl;
            stack.Children.Add(CreateSliderRow("Breeze Strength", 0.05, 1.5, _settings.BreezeIntensity, out brSlider, out brLbl, v =>
            {
                _settings.BreezeIntensity = v;
                _settings.Save();
            }, v => Math.Round(v * 100) + "%"));

            // Gravity
            Slider gvSlider; TextBlock gvLbl;
            stack.Children.Add(CreateSliderRow("Gravity Force", 5.0, 30.0, _settings.Gravity, out gvSlider, out gvLbl, v =>
            {
                _settings.Gravity = v;
                _settings.Save();
            }, v => Math.Round(v) + " m/s²"));

            // Damping (Air Resistance)
            Slider dpSlider; TextBlock dpLbl;
            stack.Children.Add(CreateSliderRow("Air Resistance", 0.94, 0.998, _settings.Damping, out dpSlider, out dpLbl, v =>
            {
                _settings.Damping = v;
                _settings.Save();
            }, v => Math.Round((1.0 - v) * 1000) + " drag"));

            // Reset Physics Button
            var resetBtn = new Button
            {
                Content = "📍 Reset Physics to Center",
                Height = 36,
                Width = 220,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 20, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 65)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            resetBtn.Click += (s, e) =>
            {
                _settings.AnchorPercentX = 0.50;
                _settings.Gravity = 16.0;
                _settings.Damping = 0.988;
                _settings.BreezeIntensity = 0.60;
                _settings.Save();
                _overlayWindow.SetAnchorPercentX(0.50);
                SwitchTab(1);
            };
            stack.Children.Add(resetBtn);

            scroll.Content = stack;
            return scroll;
        }

        #endregion

        #region Tab 2: Creator Profile (Featuring Code-Based Developer Badge)

        private UIElement BuildProfileView()
        {
            var scroll = new ScrollViewer { Margin = new Thickness(25), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var grid = new Grid { MaxWidth = 800, HorizontalAlignment = HorizontalAlignment.Left };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left: Developer Code Badge
            var photoBorder = new Border
            {
                Height = 440,
                CornerRadius = new CornerRadius(16),
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.FromRgb(16, 22, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(2)
            };
            photoBorder.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(56, 189, 248),
                BlurRadius = 16,
                Opacity = 0.4,
                ShadowDepth = 4
            };

            string avatarPath = Path.Combine(CharmItem.GetAssetsRoot(), "Assets", "psk_developer.png");
            if (File.Exists(avatarPath))
            {
                photoBorder.Child = new Image
                {
                    Source = new BitmapImage(new Uri(avatarPath, UriKind.Absolute)),
                    Stretch = Stretch.UniformToFill
                };
            }
            Grid.SetColumn(photoBorder, 0);
            grid.Children.Add(photoBorder);

            // Right: Bio & Story
            var bioStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 56, 189, 248)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            badge.Child = new TextBlock
            {
                Text = "💻 LEAD CODE BUILDER",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(125, 211, 252))
            };
            bioStack.Children.Add(badge);

            var name = new TextBlock
            {
                Text = "PSK Developer",
                FontSize = 26,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 6)
            };
            bioStack.Children.Add(name);

            var title = new TextBlock
            {
                Text = "Software Builder & Interactive Experience Designer",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            bioStack.Children.Add(title);

            var desc = new TextBlock
            {
                Text = "PSK Charms was created to bring warmth, personality, and playful motion to everyday screens. Unlike other apps that crash on missing runtimes, PSK Charms is crafted in pure native Windows WPF (.NET Framework 4.8) — launching in under 0.3s with zero external dependencies.",
                FontSize = 12.5,
                Foreground = new SolidColorBrush(Color.FromRgb(200, 205, 225)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14)
            };
            bioStack.Children.Add(desc);

            var featBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 26, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(36, 46, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 18)
            };
            var featStack = new StackPanel();
            featStack.Children.Add(new TextBlock { Text = "✨ 74+ Built-in High-Resolution Charms across 10 Collections", FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) });
            featStack.Children.Add(new TextBlock { Text = "🪢 Real Verlet Pendulum Rope Physics with Interactive Dragging", FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) });
            featStack.Children.Add(new TextBlock { Text = "🖼️ Sticker Studio: Turn Any Personal Photo into a Desktop Charm", FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) });
            featStack.Children.Add(new TextBlock { Text = "🛡️ 100% Native Windows Executable — Zero SmartScreen Warnings", FontSize = 12, Foreground = Brushes.White, Margin = new Thickness(0, 2, 0, 2) });
            featBox.Child = featStack;
            bioStack.Children.Add(featBox);

            var openWebBtn = new Button
            {
                Content = "🌐 Open Official PSK Website",
                Height = 38,
                Background = new LinearGradientBrush(
                    Color.FromRgb(56, 189, 248),
                    Color.FromRgb(59, 130, 246),
                    new Point(0, 0),
                    new Point(1, 0)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(18, 0, 18, 0)
            };
            openWebBtn.Click += (s, e) =>
            {
                try
                {
                    string htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html");
                    if (File.Exists(htmlPath))
                    {
                        Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo("http://localhost:8099") { UseShellExecute = true });
                    }
                }
                catch { }
            };
            bioStack.Children.Add(openWebBtn);

            Grid.SetColumn(bioStack, 2);
            grid.Children.Add(bioStack);

            scroll.Content = grid;
            return scroll;
        }

        #endregion

        #region Tab 3: Settings

        private UIElement BuildSettingsView()
        {
            var scroll = new ScrollViewer { Margin = new Thickness(25), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { MaxWidth = 600, HorizontalAlignment = HorizontalAlignment.Left };

            var title = new TextBlock
            {
                Text = "⚙️ System & Display Settings",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stack.Children.Add(title);

            // Always on Top
            var topCheck = new CheckBox
            {
                Content = "Always On Top (float charm above all active windows)",
                IsChecked = _settings.AlwaysOnTop,
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };
            topCheck.Click += (s, e) =>
            {
                _settings.AlwaysOnTop = (topCheck.IsChecked == true);
                _settings.Save();
                _overlayWindow.ReloadSettings();
            };
            stack.Children.Add(topCheck);

            // Click Through
            var clickCheck = new CheckBox
            {
                Content = "Click-Through Mode (clicks pass directly through charm to windows behind)",
                IsChecked = _settings.ClickThrough,
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 16)
            };
            clickCheck.Click += (s, e) =>
            {
                _settings.ClickThrough = (clickCheck.IsChecked == true);
                _settings.Save();
                _overlayWindow.ReloadSettings();
            };
            stack.Children.Add(clickCheck);

            // Start with Windows
            var startCheck = new CheckBox
            {
                Content = "Start with Windows (automatically launch PSK Charms on login)",
                IsChecked = _settings.StartWithWindows,
                Foreground = Brushes.White,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 24)
            };
            startCheck.Click += (s, e) =>
            {
                _settings.StartWithWindows = (startCheck.IsChecked == true);
                _settings.Save();
                try
                {
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        if (_settings.StartWithWindows)
                            key.SetValue("PSKCharms", "\"" + exePath + "\"");
                        else
                            key.DeleteValue("PSKCharms", false);
                    }
                }
                catch { }
            };
            stack.Children.Add(startCheck);

            // SmartScreen Fix explanation
            var infoBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 26, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(36, 46, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock { Text = "🛡️ Windows Protection & Installation Note:", FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 4) });
            infoStack.Children.Add(new TextBlock { Text = "PSK Charms is digitally signed and unblocked automatically by Run-PSK.bat. If Windows Defender ever shows 'Windows protected your PC', simply click 'More info' and 'Run anyway'.", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(180, 190, 215)), TextWrapping = TextWrapping.Wrap });
            infoBox.Child = infoStack;
            stack.Children.Add(infoBox);

            scroll.Content = stack;
            return scroll;
        }

        #endregion
    }
}
