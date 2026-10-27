using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;

namespace WattMonitor
{
    public class WattMonitorWpfWindow : Window
    {
        private WinForms.NotifyIcon trayIcon;
        private DispatcherTimer updateTimer;
        private DispatcherTimer autosaveTimer;
        private HardwareScanner hardwareScanner;

        private DateTime sessionStartTime;
        private DateTime lastUpdate;
        private double accumulatedKwh = 0;
        private double activeRunningHours = 0;
        private Dictionary<string, double> accumulatedHardwareKwh = new Dictionary<string, double>();
        private Dictionary<string, double> lastCommittedHardwareKwh = new Dictionary<string, double>();
        private double lastCommittedHours = 0;
        private double lastCommittedKwh = 0;

        private const double MAX_ELAPSED_HOURS = 5.0 / 3600.0;

        private bool isPinned = true;
        private bool isLocked = false;
        private bool isUpdatingHardware = false;
        private bool hasManualMoved = false;

        public enum ViewMode { Full, Minimalist }
        private ViewMode currentViewMode = ViewMode.Minimalist;

        // UI Root Container & Mode Panels
        private Border mainCard;
        private Border minimalistPanel;
        private Border fullPanel;

        // Minimalist HUD Elements
        private TextBlock hudTxtTotalWatt;
        private TextBlock hudTxtCpuTemp;
        private TextBlock hudTxtGpuTemp;
        private TextBlock hudTxtCpuWatt;
        private TextBlock hudTxtGpuWatt;
        private TextBlock hudTxtKwh;
        private TextBlock hudTxtCost;

        // Full Dashboard Elements
        private int currentFullPage = 1;
        private StackPanel fullPage1Container;
        private StackPanel fullPage2Container;
        private TextBlock fullTxtPageIndicator;
        private Button fullBtnPrevPage;
        private Button fullBtnNextPage;

        private TextBlock fullTxtTotalWatt;
        private TextBlock fullTxtAvgWatt;
        private TextBlock fullTxtSessionKwh;
        private TextBlock fullTxtSessionCost;
        private TextBlock fullTxtCpuTemp;
        private TextBlock fullTxtCpuWatt;
        private ProgressBar fullPbCpuWatt;
        private TextBlock fullTxtGpuTemp;
        private TextBlock fullTxtGpuWatt;
        private ProgressBar fullPbGpuWatt;
        private TextBlock fullTxtMonitorName;
        private TextBlock fullTxtMonitorWatt;
        private ProgressBar fullPbMonitorWatt;
        private TextBlock fullTxtCostPerHour;
        private TextBlock fullTxtCostPerDay;
        private TextBlock fullTxtCostPerMonth;
        private StackPanel fullHwSensorsContainer;
        private TextBlock fullTxtSessionDuration;
        private Button fullBtnPin;
        private Button fullBtnLock;

        public WattMonitorWpfWindow()
        {
            this.Title = "WattMonitor";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.ShowInTaskbar = false;
            this.Topmost = isPinned;
            this.SizeToContent = SizeToContent.WidthAndHeight;
            this.ResizeMode = ResizeMode.NoResize;

            Config.Load();
            this.Opacity = (currentViewMode == ViewMode.Minimalist) ? Config.MinimalistOpacity : Config.Opacity;

            // Load window icon from assets or executable
            try
            {
                string iconPng = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.png");
                string iconIco = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "wattmonitor.ico");
                if (File.Exists(iconPng)) this.Icon = new BitmapImage(new Uri(iconPng, UriKind.Absolute));
                else if (File.Exists(iconIco)) this.Icon = new BitmapImage(new Uri(iconIco, UriKind.Absolute));
            }
            catch { }

            BuildUI();
            InitTrayIcon();
            InitHardwareAndTimers();

            this.ContentRendered += (s, e) => PositionWindow();
            this.SizeChanged += (s, e) => {
                if (!hasManualMoved) PositionWindow();
            };

            this.KeyDown += (s, e) =>
            {
                if (currentViewMode == ViewMode.Full)
                {
                    if (e.Key == System.Windows.Input.Key.Left || e.Key == System.Windows.Input.Key.PageUp)
                    {
                        SwitchFullPage(1);
                        e.Handled = true;
                    }
                    else if (e.Key == System.Windows.Input.Key.Right || e.Key == System.Windows.Input.Key.PageDown)
                    {
                        SwitchFullPage(2);
                        e.Handled = true;
                    }
                }
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TOOLWINDOW);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal mengatur WS_EX_TOOLWINDOW", ex);
            }
        }

        private void BuildUI()
        {
            mainCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)), // #0B0F19 (Dark Slate Canvas)
                BorderBrush = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)), // #1E293B
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(0),
                SnapsToDevicePixels = true
            };

            // Drag Window Handler
            mainCard.MouseDown += (s, e) =>
            {
                if (!isLocked && e.ChangedButton == MouseButton.Left)
                {
                    hasManualMoved = true;
                    this.DragMove();
                }
            };

            // Double Click to Toggle View Mode
            mainCard.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    ToggleViewMode();
                }
            };

            // Custom Modern Context Menu on Card
            mainCard.ContextMenu = null;
            mainCard.MouseRightButtonUp += (s, e) =>
            {
                e.Handled = true;
                ShowModernContextMenu(false);
            };

            // Build Both Panels
            BuildMinimalistPanel();
            BuildFullPanel();

            Grid rootGrid = new Grid();
            rootGrid.Children.Add(minimalistPanel);
            rootGrid.Children.Add(fullPanel);
            mainCard.Child = rootGrid;

            this.Content = mainCard;
            ApplyViewMode(currentViewMode);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private void ShowModernContextMenu(bool fromTray)
        {
            ContextMenu cm = new ContextMenu
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderThickness = new Thickness(1),
                MinWidth = 225,
                HasDropShadow = true
            };

            ControlTemplate cmTemplate = new ControlTemplate(typeof(ContextMenu));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(11, 15, 25)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(30, 41, 59)));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(4));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.IsItemsHostProperty, true);
            borderFactory.AppendChild(stackPanelFactory);

            cmTemplate.VisualTree = borderFactory;
            cm.Template = cmTemplate;

            // 1. Minimalist HUD
            cm.Items.Add(CreateModernMenuItem("Minimalist HUD", LucideIcon.Activity, () => SetViewMode(ViewMode.Minimalist), new SolidColorBrush(Color.FromRgb(56, 189, 248)), true, currentViewMode == ViewMode.Minimalist));

            // 2. Full Dashboard
            cm.Items.Add(CreateModernMenuItem("Full Dashboard", LucideIcon.BarChart, () => SetViewMode(ViewMode.Full), new SolidColorBrush(Color.FromRgb(245, 158, 11)), true, currentViewMode == ViewMode.Full));

            cm.Items.Add(CreateModernMenuSeparator());

            // 3. Pin to Top
            cm.Items.Add(CreateModernMenuItem("Pin to Top (Di Depan)", LucideIcon.Pin, () => TogglePin(), isPinned ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : new SolidColorBrush(Color.FromRgb(148, 163, 184)), true, isPinned));

            // 4. Kunci Posisi
            cm.Items.Add(CreateModernMenuItem("Kunci Posisi (Lock)", LucideIcon.Lock, () => ToggleLock(), isLocked ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) : new SolidColorBrush(Color.FromRgb(148, 163, 184)), true, isLocked));

            cm.Items.Add(CreateModernMenuSeparator());

            // 5. Lihat Laporan
            cm.Items.Add(CreateModernMenuItem("Lihat Laporan", LucideIcon.BarChart, () => OpenReportViewer(0), new SolidColorBrush(Color.FromRgb(56, 189, 248))));

            // 6. Pengaturan
            cm.Items.Add(CreateModernMenuItem("Pengaturan", LucideIcon.Settings, () => OpenSettingsWindow(), new SolidColorBrush(Color.FromRgb(168, 85, 247))));

            cm.Items.Add(CreateModernMenuSeparator());

            // 7. Keluar
            cm.Items.Add(CreateModernMenuItem("Keluar WattMonitor", LucideIcon.LogOut, () =>
            {
                if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
                System.Windows.Application.Current.Shutdown();
            }, new SolidColorBrush(Color.FromRgb(248, 113, 113)), false, false, true));

            if (fromTray)
            {
                IntPtr handle = new WindowInteropHelper(this).Handle;
                SetForegroundWindow(handle);
                cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            }
            else
            {
                cm.PlacementTarget = mainCard;
                cm.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            }

            cm.IsOpen = true;
        }

        private MenuItem CreateModernMenuItem(string title, string iconData, Action onClick, Brush iconBrush = null, bool isCheckable = false, bool isChecked = false, bool isDanger = false)
        {
            MenuItem mi = new MenuItem
            {
                Height = 32,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 1, 0, 1)
            };

            ControlTemplate ct = new ControlTemplate(typeof(MenuItem));
            FrameworkElementFactory itemBorder = new FrameworkElementFactory(typeof(Border), "ItemBorder");
            itemBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            itemBorder.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
            itemBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            itemBorder.SetValue(Border.SnapsToDevicePixelsProperty, true);

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            itemBorder.AppendChild(cp);
            ct.VisualTree = itemBorder;

            Trigger trHighlight = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            Color hoverColor = isDanger ? Color.FromArgb(50, 239, 68, 68) : Color.FromRgb(30, 41, 59);
            trHighlight.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverColor), "ItemBorder"));
            ct.Triggers.Add(trHighlight);

            mi.Template = ct;

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            FrameworkElement iconElem = LucideIcon.Create(iconData, 13, iconBrush ?? Brushes.White, false, 1.8);
            Grid.SetColumn(iconElem, 0);
            g.Children.Add(iconElem);

            TextBlock tb = new TextBlock
            {
                Text = title,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontSize = 11.5,
                FontWeight = isChecked ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = isDanger ? new SolidColorBrush(Color.FromRgb(248, 113, 113)) : (isChecked ? Brushes.White : new SolidColorBrush(Color.FromRgb(226, 232, 240))),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 8, 0)
            };
            Grid.SetColumn(tb, 1);
            g.Children.Add(tb);

            if (isCheckable && isChecked)
            {
                FrameworkElement checkIcon = LucideIcon.Create(LucideIcon.Check, 12, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false, 2.4);
                Grid.SetColumn(checkIcon, 2);
                g.Children.Add(checkIcon);
            }

            mi.Header = g;
            mi.Click += (s, e) => onClick();
            return mi;
        }

        private Separator CreateModernMenuSeparator()
        {
            Separator sep = new Separator
            {
                Margin = new Thickness(6, 3, 6, 3),
                Focusable = false,
                IsHitTestVisible = false
            };
            ControlTemplate template = new ControlTemplate(typeof(Separator));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.HeightProperty, 1.0);
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(90, 51, 65, 85)));
            template.VisualTree = border;
            sep.Template = template;
            return sep;
        }

        private void BuildMinimalistPanel()
        {
            minimalistPanel = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(14, 7, 14, 7)
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Zap Icon + Total Watt
            sp.Children.Add(LucideIcon.Create(LucideIcon.Zap, 14, new SolidColorBrush(Color.FromRgb(245, 158, 11)), true)); // Amber
            sp.Children.Add(new Border { Width = 6 });

            hudTxtTotalWatt = new TextBlock
            {
                Text = "0",
                FontFamily = new FontFamily("Segoe UI, Inter, Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(hudTxtTotalWatt);

            TextBlock unitWatt = new TextBlock { Text = "W", FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(1, 2, 8, 0) };
            sp.Children.Add(unitWatt);

            sp.Children.Add(CreateHudSeparator());

            // CPU Stat
            TextBlock lblCpu = new TextBlock { Text = "C:", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(lblCpu);
            hudTxtCpuTemp = new TextBlock { Text = "0°", FontSize = 11, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold, Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 4, 0) };
            sp.Children.Add(hudTxtCpuTemp);
            hudTxtCpuWatt = new TextBlock { Text = "0W", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            sp.Children.Add(hudTxtCpuWatt);

            sp.Children.Add(CreateHudSeparator());

            // GPU Stat
            TextBlock lblGpu = new TextBlock { Text = "G:", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(lblGpu);
            hudTxtGpuTemp = new TextBlock { Text = "0°", FontSize = 11, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold, Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 4, 0) };
            sp.Children.Add(hudTxtGpuTemp);
            hudTxtGpuWatt = new TextBlock { Text = "0W", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            sp.Children.Add(hudTxtGpuWatt);

            sp.Children.Add(CreateHudSeparator());

            // kWh
            hudTxtKwh = new TextBlock { Text = "0.0000 kWh", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
            sp.Children.Add(hudTxtKwh);

            sp.Children.Add(CreateHudSeparator());

            // Rupiah
            hudTxtCost = new TextBlock { Text = "Rp 0,00", FontSize = 11, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)), VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(hudTxtCost);

            minimalistPanel.Child = sp;
        }

        private Border CreateHudSeparator()
        {
            return new Border
            {
                Width = 1,
                Height = 12,
                Background = new SolidColorBrush(Color.FromArgb(120, 71, 85, 105)),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void BuildFullPanel()
        {
            fullPanel = new Border
            {
                Background = Brushes.Transparent,
                Width = 380,
                Padding = new Thickness(14)
            };

            StackPanel spRoot = new StackPanel();

            // Header Bar
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleSp.Children.Add(LucideIcon.Create(LucideIcon.Zap, 15, new SolidColorBrush(Color.FromRgb(245, 158, 11)), true));
            titleSp.Children.Add(new TextBlock
            {
                Text = " WattMonitor",
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            fullTxtSessionDuration = new TextBlock
            {
                Text = " · 00:00:00",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(fullTxtSessionDuration);
            Grid.SetColumn(titleSp, 0);
            headerGrid.Children.Add(titleSp);

            // Controls (Report, Pin, Lock, Minimize, Close)
            StackPanel actionsSp = new StackPanel { Orientation = Orientation.Horizontal };

            Button btnReport = CreateIconButton(LucideIcon.BarChart, "Buka Laporan Pemakaian & Biaya", () => OpenReportViewer(0));
            actionsSp.Children.Add(btnReport);

            Button btnSettings = CreateIconButton(LucideIcon.Settings, "Pengaturan", () => OpenSettingsWindow());
            actionsSp.Children.Add(btnSettings);
            
            fullBtnPin = CreateIconButton(LucideIcon.Pin, "Pin to Top", () => TogglePin(), isPinned);
            actionsSp.Children.Add(fullBtnPin);

            fullBtnLock = CreateIconButton(LucideIcon.Lock, "Kunci Posisi", () => ToggleLock(), isLocked);
            actionsSp.Children.Add(fullBtnLock);

            Button btnHud = CreateIconButton(LucideIcon.Minimize, "Mode Minimalist HUD", () => SetViewMode(ViewMode.Minimalist));
            actionsSp.Children.Add(btnHud);

            Button btnClose = CreateIconButton(LucideIcon.X, "Tutup ke System Tray", () => this.Hide());
            actionsSp.Children.Add(btnClose);

            Grid.SetColumn(actionsSp, 1);
            headerGrid.Children.Add(actionsSp);
            spRoot.Children.Add(headerGrid);

            // Navigation Bar (Pagination < Page 1 / 2 >)
            Border navBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 0, 10)
            };

            Grid navGrid = new Grid();
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            fullBtnPrevPage = CreateIconButton(LucideIcon.ChevronLeft, "Halaman Sebelumnya (Alt+Left)", () => SwitchFullPage(currentFullPage == 1 ? 2 : 1));
            Grid.SetColumn(fullBtnPrevPage, 0);
            navGrid.Children.Add(fullBtnPrevPage);

            fullTxtPageIndicator = new TextBlock
            {
                Text = "1 / 2 · KONSUMSI & BIAYA",
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(fullTxtPageIndicator, 1);
            navGrid.Children.Add(fullTxtPageIndicator);

            fullBtnNextPage = CreateIconButton(LucideIcon.ChevronRight, "Halaman Berikutnya (Alt+Right)", () => SwitchFullPage(currentFullPage == 1 ? 2 : 1));
            Grid.SetColumn(fullBtnNextPage, 2);
            navGrid.Children.Add(fullBtnNextPage);

            navBar.Child = navGrid;
            spRoot.Children.Add(navBar);

            // ================= PAGE 1: KONSUMSI, BIAYA & RINCIAN =================
            fullPage1Container = new StackPanel();

            // Primary Watt Hero Card (Konsumsi)
            Border heroCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };

            Grid heroGrid = new Grid();
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            heroGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Hero: Live Watt
            StackPanel liveWattSp = new StackPanel();
            liveWattSp.Children.Add(new TextBlock { Text = "KONSUMSI SEKARANG", FontSize = 9.5, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) });
            
            StackPanel liveValSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
            fullTxtTotalWatt = new TextBlock { Text = "0", FontSize = 25, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.ExtraBold, Foreground = Brushes.White };
            liveValSp.Children.Add(fullTxtTotalWatt);
            liveValSp.Children.Add(new TextBlock { Text = " W", FontSize = 13, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 4) });
            liveWattSp.Children.Add(liveValSp);
            Grid.SetColumn(liveWattSp, 0);
            heroGrid.Children.Add(liveWattSp);

            // Right Hero: Avg Watt & Sesi
            StackPanel avgWattSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            avgWattSp.Children.Add(new TextBlock { Text = "RATA-RATA SESI", FontSize = 9.5, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), HorizontalAlignment = HorizontalAlignment.Right });
            
            StackPanel avgValSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
            fullTxtAvgWatt = new TextBlock { Text = "0", FontSize = 23, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)) };
            avgValSp.Children.Add(fullTxtAvgWatt);
            avgValSp.Children.Add(new TextBlock { Text = " W", FontSize = 13, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 4) });
            avgWattSp.Children.Add(avgValSp);
            Grid.SetColumn(avgWattSp, 1);
            heroGrid.Children.Add(avgWattSp);

            heroCard.Child = heroGrid;
            fullPage1Container.Children.Add(heroCard);

            // Cost & Projections Card (Biaya & Rincian)
            Border costCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 4)
            };

            StackPanel costSp = new StackPanel();

            // Total Biaya Sesi
            Grid costHeader = new Grid();
            costHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            costHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel costTitleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            costTitleSp.Children.Add(new TextBlock { Text = "Biaya Listrik Sesi", FontSize = 12, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            fullTxtSessionKwh = new TextBlock { Text = " (0.0000 kWh)", FontSize = 10.5, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), Margin = new Thickness(4, 1, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            costTitleSp.Children.Add(fullTxtSessionKwh);
            Grid.SetColumn(costTitleSp, 0);
            costHeader.Children.Add(costTitleSp);

            fullTxtSessionCost = new TextBlock { Text = "Rp 0,00", FontSize = 15, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)) };
            Grid.SetColumn(fullTxtSessionCost, 1);
            costHeader.Children.Add(fullTxtSessionCost);
            costSp.Children.Add(costHeader);

            costSp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)), Margin = new Thickness(0, 8, 0, 8) });

            // 3-Row Projections: Jam | Hari | Bulan (Rincian)
            Grid projGrid = new Grid();
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            projGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Per Jam
            StackPanel spJam = new StackPanel();
            spJam.Children.Add(new TextBlock { Text = "Per Jam", FontSize = 10, FontFamily = new FontFamily("Segoe UI, Inter"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) });
            fullTxtCostPerHour = new TextBlock { Text = "Rp 0", FontSize = 12.5, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 3, 0, 0) };
            spJam.Children.Add(fullTxtCostPerHour);
            Grid.SetColumn(spJam, 0);
            projGrid.Children.Add(spJam);

            // Per Hari
            StackPanel spHari = new StackPanel();
            spHari.Children.Add(new TextBlock { Text = string.Format("Hari ({0}h)", Config.HoursPerDay), FontSize = 10, FontFamily = new FontFamily("Segoe UI, Inter"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) });
            fullTxtCostPerDay = new TextBlock { Text = "Rp 0", FontSize = 12.5, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 3, 0, 0) };
            spHari.Children.Add(fullTxtCostPerDay);
            Grid.SetColumn(spHari, 1);
            projGrid.Children.Add(spHari);

            // Per Bulan
            StackPanel spBulan = new StackPanel();
            spBulan.Children.Add(new TextBlock { Text = string.Format("Bulan ({0}h)", Config.HoursPerMonth), FontSize = 10, FontFamily = new FontFamily("Segoe UI, Inter"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) });
            fullTxtCostPerMonth = new TextBlock { Text = "Rp 0", FontSize = 12.5, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 3, 0, 0) };
            spBulan.Children.Add(fullTxtCostPerMonth);
            Grid.SetColumn(spBulan, 2);
            projGrid.Children.Add(spBulan);

            costSp.Children.Add(projGrid);
            costCard.Child = costSp;
            fullPage1Container.Children.Add(costCard);

            spRoot.Children.Add(fullPage1Container);

            // ================= PAGE 2: KOMPONEN (CPU / GPU / MONITOR / HARDWARE LAINNYA) =================
            fullPage2Container = new StackPanel { Visibility = Visibility.Collapsed };

            // CPU & GPU 2-Column Metrics Card
            Grid hwGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            hwGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hwGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            hwGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // CPU Box
            Border cpuBox = CreateHardwareMetricBox("Processor (CPU)", out fullTxtCpuTemp, out fullTxtCpuWatt, out fullPbCpuWatt, Color.FromRgb(59, 130, 246));
            Grid.SetColumn(cpuBox, 0);
            hwGrid.Children.Add(cpuBox);

            // GPU Box
            Border gpuBox = CreateHardwareMetricBox("Graphics (GPU)", out fullTxtGpuTemp, out fullTxtGpuWatt, out fullPbGpuWatt, Color.FromRgb(168, 85, 247));
            Grid.SetColumn(gpuBox, 2);
            hwGrid.Children.Add(gpuBox);

            fullPage2Container.Children.Add(hwGrid);

            // Monitor Card
            Border monitorCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel monitorSp = new StackPanel();

            Grid monitorHeader = new Grid();
            monitorHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            monitorHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel monitorTitleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            monitorTitleSp.Children.Add(LucideIcon.Create(LucideIcon.Monitor, 14, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false));
            fullTxtMonitorName = new TextBlock
            {
                Text = " Layar / Monitor",
                FontSize = 11.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            monitorTitleSp.Children.Add(fullTxtMonitorName);
            Grid.SetColumn(monitorTitleSp, 0);
            monitorHeader.Children.Add(monitorTitleSp);

            fullTxtMonitorWatt = new TextBlock
            {
                Text = "0.0 W",
                FontSize = 14,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(fullTxtMonitorWatt, 1);
            monitorHeader.Children.Add(fullTxtMonitorWatt);

            monitorSp.Children.Add(monitorHeader);

            fullPbMonitorWatt = new ProgressBar
            {
                Height = 4,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Background = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 6, 0, 0)
            };
            monitorSp.Children.Add(fullPbMonitorWatt);

            monitorCard.Child = monitorSp;
            fullPage2Container.Children.Add(monitorCard);

            // Expandable Per-Hardware List Container (Komponen & Sensor Lainnya)
            TextBlock hwHeaderTitle = new TextBlock
            {
                Text = "KOMPONEN & SENSOR LAINNYA",
                FontSize = 9.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 0, 0, 6)
            };
            fullPage2Container.Children.Add(hwHeaderTitle);

            fullHwSensorsContainer = new StackPanel();
            ScrollViewer svSensors = new ScrollViewer
            {
                Content = fullHwSensorsContainer,
                MaxHeight = 125,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 0, 0, 4)
            };
            fullPage2Container.Children.Add(svSensors);

            spRoot.Children.Add(fullPage2Container);

            fullPanel.Child = spRoot;
        }

        private void SwitchFullPage(int targetPage)
        {
            currentFullPage = targetPage;
            if (currentFullPage == 1)
            {
                if (fullPage1Container != null) fullPage1Container.Visibility = Visibility.Visible;
                if (fullPage2Container != null) fullPage2Container.Visibility = Visibility.Collapsed;
                if (fullTxtPageIndicator != null)
                {
                    fullTxtPageIndicator.Text = "1 / 2 · KONSUMSI & BIAYA";
                    fullTxtPageIndicator.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248));
                }
            }
            else
            {
                if (fullPage1Container != null) fullPage1Container.Visibility = Visibility.Collapsed;
                if (fullPage2Container != null) fullPage2Container.Visibility = Visibility.Visible;
                if (fullTxtPageIndicator != null)
                {
                    fullTxtPageIndicator.Text = "2 / 2 · KOMPONEN HARDWARE";
                    fullTxtPageIndicator.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
                }
            }
            if (!hasManualMoved) PositionWindow();
        }

        private Border CreateHardwareMetricBox(string title, out TextBlock txtTemp, out TextBlock txtWatt, out ProgressBar pbWatt, Color accent)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8)
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = title, FontSize = 11, FontFamily = new FontFamily("Segoe UI, Inter"), FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(accent) });

            Grid valGrid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            valGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            valGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            txtTemp = new TextBlock { Text = "--°C", FontSize = 14, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            Grid.SetColumn(txtTemp, 0);
            valGrid.Children.Add(txtTemp);

            txtWatt = new TextBlock { Text = "0.0 W", FontSize = 14, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = Brushes.White };
            Grid.SetColumn(txtWatt, 1);
            valGrid.Children.Add(txtWatt);

            sp.Children.Add(valGrid);

            pbWatt = new ProgressBar
            {
                Height = 4,
                Minimum = 0,
                Maximum = 150,
                Value = 0,
                Foreground = new SolidColorBrush(accent),
                Background = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(0)
            };
            sp.Children.Add(pbWatt);

            b.Child = sp;
            return b;
        }

        private Button CreateIconButton(string iconPath, string tooltip, Action onClick, bool isActive = false)
        {
            Button btn = new Button
            {
                Width = 26,
                Height = 26,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0),
                ToolTip = tooltip,
                Focusable = false
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "bd";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(140, 51, 65, 85)), "bd"));
            template.Triggers.Add(hoverTrigger);

            Trigger pressTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(200, 71, 85, 105)), "bd"));
            template.Triggers.Add(pressTrigger);

            btn.Template = template;
            Brush stroke = isActive ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
            btn.Content = LucideIcon.Create(iconPath, 13, stroke, false, 1.8);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void InitTrayIcon()
        {
            trayIcon = new WinForms.NotifyIcon();
            trayIcon.Text = "WattMonitor";

            try
            {
                trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            }
            catch
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "wattmonitor.ico");
                if (File.Exists(iconPath)) trayIcon.Icon = new System.Drawing.Icon(iconPath);
                else trayIcon.Icon = System.Drawing.SystemIcons.Information;
            }

            trayIcon.Visible = true;
            trayIcon.ContextMenu = null;

            trayIcon.MouseClick += (s, e) =>
            {
                if (e.Button == WinForms.MouseButtons.Left)
                {
                    if (this.IsVisible) this.Hide();
                    else
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                    }
                }
                else if (e.Button == WinForms.MouseButtons.Right)
                {
                    this.Dispatcher.Invoke(() => ShowModernContextMenu(true));
                }
            };

            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };
        }

        private void TogglePin()
        {
            isPinned = !isPinned;
            this.Topmost = isPinned;
            if (fullBtnPin != null)
            {
                Brush stroke = isPinned ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                fullBtnPin.Content = LucideIcon.Create(LucideIcon.Pin, 13, stroke, false, 1.8);
            }
        }

        private void ToggleLock()
        {
            isLocked = !isLocked;
            if (fullBtnLock != null)
            {
                Brush stroke = isLocked ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) : new SolidColorBrush(Color.FromRgb(148, 163, 184));
                fullBtnLock.Content = LucideIcon.Create(LucideIcon.Lock, 13, stroke, false, 1.8);
            }
        }

        public void ToggleViewMode()
        {
            SetViewMode(currentViewMode == ViewMode.Minimalist ? ViewMode.Full : ViewMode.Minimalist);
        }

        public void SetViewMode(ViewMode mode)
        {
            currentViewMode = mode;
            ApplyViewMode(mode);
            PositionWindow();
        }

        private void ApplyViewMode(ViewMode mode)
        {
            if (mode == ViewMode.Minimalist)
            {
                fullPanel.Visibility = Visibility.Collapsed;
                minimalistPanel.Visibility = Visibility.Visible;
                this.Opacity = Config.MinimalistOpacity;
                mainCard.CornerRadius = new CornerRadius(18); // Perfect semi-circle pill for ~36px height
            }
            else
            {
                minimalistPanel.Visibility = Visibility.Collapsed;
                fullPanel.Visibility = Visibility.Visible;
                this.Opacity = Config.Opacity;
                mainCard.CornerRadius = new CornerRadius(12); // Sleek rounded Dashboard Card
            }
        }

        private void PositionWindow()
        {
            this.UpdateLayout();
            double w = this.ActualWidth;
            double h = this.ActualHeight;

            if (w <= 0 || h <= 0)
            {
                UIElement content = this.Content as UIElement;
                if (content != null)
                {
                    content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    w = content.DesiredSize.Width;
                    h = content.DesiredSize.Height;
                }
            }

            if (w <= 0) w = (currentViewMode == ViewMode.Minimalist) ? 370 : 380;
            if (h <= 0) h = (currentViewMode == ViewMode.Minimalist) ? 36 : 420;

            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - w - 16;
            this.Top = workingArea.Bottom - h - 14;
        }

        private ReportViewerWindow reportViewerWindow;
        private SettingsWindow settingsWindow;
        private double lastTotalWatt = 0;

        public LiveSessionSnapshot GetLiveSnapshot()
        {
            double uncommittedHours = Math.Max(0, activeRunningHours - lastCommittedHours);
            double uncommittedKwh = Math.Max(0, accumulatedKwh - lastCommittedKwh);
            var uncommittedHw = new Dictionary<string, double>();
            foreach (var kvp in accumulatedHardwareKwh)
            {
                double last = 0;
                lastCommittedHardwareKwh.TryGetValue(kvp.Key, out last);
                double d = kvp.Value - last;
                if (d > 0) uncommittedHw[kvp.Key] = d;
            }
            return new LiveSessionSnapshot
            {
                SessionStartTime = sessionStartTime,
                ActiveRunningHours = activeRunningHours,
                AccumulatedKwh = accumulatedKwh,
                UncommittedHours = uncommittedHours,
                UncommittedKwh = uncommittedKwh,
                UncommittedCost = uncommittedKwh * Config.Tariff,
                UncommittedHardwareKwh = uncommittedHw,
                CurrentWatt = lastTotalWatt
            };
        }

        public void OpenReportViewer(int initialTab = 0)
        {
            try
            {
                if (reportViewerWindow == null || !reportViewerWindow.IsLoaded)
                {
                    reportViewerWindow = new ReportViewerWindow(initialTab, GetLiveSnapshot);
                    reportViewerWindow.Closed += (s, e) => reportViewerWindow = null;
                    reportViewerWindow.Show();
                }
                else
                {
                    reportViewerWindow.SelectTab(initialTab);
                    if (reportViewerWindow.WindowState == WindowState.Minimized)
                        reportViewerWindow.WindowState = WindowState.Normal;
                    reportViewerWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal membuka ReportViewerWindow", ex);
            }
        }

        public void OpenSettingsWindow()
        {
            try
            {
                if (settingsWindow == null || !settingsWindow.IsLoaded)
                {
                    settingsWindow = new SettingsWindow(() =>
                    {
                        ApplyConfigChanges();
                    });
                    settingsWindow.Closed += (s, e) => settingsWindow = null;
                    settingsWindow.Show();
                }
                else
                {
                    if (settingsWindow.WindowState == WindowState.Minimized)
                        settingsWindow.WindowState = WindowState.Normal;
                    settingsWindow.Activate();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal membuka SettingsWindow", ex);
            }
        }

        public void ApplyConfigChanges()
        {
            try
            {
                if (currentViewMode == ViewMode.Full)
                {
                    this.Opacity = Config.Opacity;
                }
                else
                {
                    this.Opacity = Config.MinimalistOpacity;
                }

                if (hudTxtTotalWatt != null)
                {
                    hudTxtTotalWatt.FontSize = Config.MinimalistFontSize + 2;
                    if (hudTxtCpuTemp != null) hudTxtCpuTemp.FontSize = Config.MinimalistFontSize;
                    if (hudTxtCpuWatt != null) hudTxtCpuWatt.FontSize = Config.MinimalistFontSize;
                    if (hudTxtGpuTemp != null) hudTxtGpuTemp.FontSize = Config.MinimalistFontSize;
                    if (hudTxtGpuWatt != null) hudTxtGpuWatt.FontSize = Config.MinimalistFontSize;
                    if (hudTxtKwh != null) hudTxtKwh.FontSize = Config.MinimalistFontSize;
                    if (hudTxtCost != null) hudTxtCost.FontSize = Config.MinimalistFontSize;
                }

                if (updateTimer != null && (int)updateTimer.Interval.TotalMilliseconds != Config.RefreshRateMs)
                {
                    updateTimer.Interval = TimeSpan.FromMilliseconds(Config.RefreshRateMs);
                }

                if (autosaveTimer != null && Math.Abs(autosaveTimer.Interval.TotalMinutes - Config.AutosaveMinutes) > 0.001)
                {
                    autosaveTimer.Interval = TimeSpan.FromMinutes(Config.AutosaveMinutes);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal menerapkan update konfigurasi", ex);
            }
        }

        private void OpenSessionReport()
        {
            OpenReportViewer(0);
        }

        private void OpenHardwareReport()
        {
            OpenReportViewer(1);
        }

        private void InitHardwareAndTimers()
        {
            hardwareScanner = new HardwareScanner();
            try
            {
                hardwareScanner.Open();
                AppLogger.Info("HardwareScanner WPF berhasil dibuka.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal membuka HardwareScanner di WPF.", ex);
                WinForms.MessageBox.Show("Gagal mengakses sensor hardware. Pastikan Run as Administrator!\n\n" + ex.Message,
                    "Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                System.Windows.Application.Current.Shutdown();
                return;
            }

            sessionStartTime = DateTime.Now;
            SessionManager.LoadHistoricalLogs(sessionStartTime);
            AppLogger.Info(string.Format("Sesi WPF dimulai: {0:yyyy-MM-dd HH:mm:ss} | RefreshRate={1}ms Tariff={2}", sessionStartTime, Config.RefreshRateMs, Config.Tariff));
            SessionManager.WriteSessionLog(sessionStartTime, 0, 0, 0, null);
            lastUpdate = DateTime.Now;

            updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
            updateTimer.Interval = TimeSpan.FromMilliseconds(Config.RefreshRateMs);
            updateTimer.Tick += UpdateLoop;
            updateTimer.Start();

            if (Config.AutosaveMinutes > 0)
            {
                autosaveTimer = new DispatcherTimer(DispatcherPriority.Background);
                autosaveTimer.Interval = TimeSpan.FromMinutes(Math.Max(0.5, Config.AutosaveMinutes));
                autosaveTimer.Tick += (s, e) =>
                {
                    double deltaHours = activeRunningHours - lastCommittedHours;
                    double deltaKwh = accumulatedKwh - lastCommittedKwh;
                    var deltaHw = GetHardwareDeltas();

                    if (deltaHours > 0 || deltaKwh > 0 || deltaHw.Count > 0)
                    {
                        SessionManager.WriteSessionLog(sessionStartTime, deltaHours * 60.0, deltaKwh, deltaKwh * Config.Tariff, deltaHw);
                        lastCommittedHours = activeRunningHours;
                        lastCommittedKwh = accumulatedKwh;
                        CommitHardwareDeltas();
                    }
                };
                autosaveTimer.Start();
            }
        }

        private Dictionary<string, double> GetHardwareDeltas()
        {
            var deltas = new Dictionary<string, double>();
            foreach (var kvp in accumulatedHardwareKwh)
            {
                double last = 0;
                lastCommittedHardwareKwh.TryGetValue(kvp.Key, out last);
                double delta = kvp.Value - last;
                if (delta > 0) deltas[kvp.Key] = delta;
            }
            return deltas;
        }

        private void CommitHardwareDeltas()
        {
            foreach (var kvp in accumulatedHardwareKwh)
            {
                lastCommittedHardwareKwh[kvp.Key] = kvp.Value;
            }
        }

        private async void UpdateLoop(object sender, EventArgs e)
        {
            if (isUpdatingHardware) return;
            isUpdatingHardware = true;
            try
            {
                var res = await System.Threading.Tasks.Task.Run(() => hardwareScanner.Update(Config.ExcludeTexts, Config.StaticPeripherals));
                DateTime now = DateTime.Now;

                // Day rollover check
                if (now.Date != sessionStartTime.Date)
                {
                    AppLogger.Info(string.Format("Day rollover WPF: {0:yyyy-MM-dd} -> {1:yyyy-MM-dd} | kWh={2:F4}", sessionStartTime, now, accumulatedKwh));
                    AppLogger.PurgeOldLogs();

                    double deltaHours = activeRunningHours - lastCommittedHours;
                    double deltaKwh = accumulatedKwh - lastCommittedKwh;
                    var deltaHw = GetHardwareDeltas();

                    SessionManager.WriteSessionLog(sessionStartTime, deltaHours * 60.0, deltaKwh, deltaKwh * Config.Tariff, deltaHw);

                    if (now.Month != sessionStartTime.Month || now.Year != sessionStartTime.Year)
                    {
                        SessionManager.LoadHistoricalLogs(now);
                    }

                    sessionStartTime = now;
                    accumulatedKwh = 0;
                    activeRunningHours = 0;
                    lastCommittedHours = 0;
                    lastCommittedKwh = 0;
                    accumulatedHardwareKwh.Clear();
                    lastCommittedHardwareKwh.Clear();
                }

                double elapsed = (now - lastUpdate).TotalHours;
                lastUpdate = now;

                if (elapsed <= MAX_ELAPSED_HOURS)
                {
                    accumulatedKwh += (res.CurrentTotalWatt / 1000.0) * elapsed;
                    activeRunningHours += elapsed;

                    foreach (var hw in res.ActiveSensors)
                    {
                        if (!accumulatedHardwareKwh.ContainsKey(hw.Name)) accumulatedHardwareKwh[hw.Name] = 0;
                        accumulatedHardwareKwh[hw.Name] += (hw.Watt / 1000.0) * elapsed;
                    }
                    foreach (var st in Config.StaticPeripherals)
                    {
                        if (!accumulatedHardwareKwh.ContainsKey(st.Key)) accumulatedHardwareKwh[st.Key] = 0;
                        accumulatedHardwareKwh[st.Key] += (st.Value / 1000.0) * elapsed;
                    }
                }

                lastTotalWatt = res.CurrentTotalWatt;
                double avgWatt = activeRunningHours > 0 ? (accumulatedKwh * 1000.0) / activeRunningHours : 0;
                double sessionCost = accumulatedKwh * Config.Tariff;
                double costPerHour = (avgWatt / 1000.0) * Config.Tariff;
                double costPerDay = costPerHour * Config.HoursPerDay;
                double costPerMonth = costPerHour * Config.HoursPerMonth;

                string tip = string.Format("Total Biaya: Rp {0:N2}", sessionCost);
                if (trayIcon != null)
                {
                    trayIcon.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
                }

                if (this.IsVisible)
                {
                    RenderWpfUI(res.CurrentTotalWatt, avgWatt, sessionCost, costPerHour, costPerDay, costPerMonth, res.ActiveSensors);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Error di UpdateLoop WPF", ex);
            }
            finally
            {
                isUpdatingHardware = false;
            }
        }

        private void RenderWpfUI(double totalWatt, double avgWatt, double sessionCost, double costPerHour, double costPerDay, double costPerMonth, List<HardwareSensorData> sensors)
        {
            double? cpuTemp = null, gpuTemp = null;
            double cpuWatt = 0, gpuWatt = 0;

            foreach (var s in sensors)
            {
                if (s.Type == HardwareType.Cpu)
                {
                    if (s.Temp.HasValue && (!cpuTemp.HasValue || s.Temp.Value > cpuTemp.Value)) cpuTemp = s.Temp.Value;
                    cpuWatt += s.Watt;
                }
                else if (s.Type == HardwareType.GpuNvidia || s.Type == HardwareType.GpuAmd || s.Type == HardwareType.GpuIntel)
                {
                    if (s.Temp.HasValue && (!gpuTemp.HasValue || s.Temp.Value > gpuTemp.Value)) gpuTemp = s.Temp.Value;
                    gpuWatt += s.Watt;
                }
            }

            // 1. UPDATE MINIMALIST HUD
            if (currentViewMode == ViewMode.Minimalist)
            {
                hudTxtTotalWatt.Text = string.Format("{0:F0}", totalWatt);
                hudTxtTotalWatt.Foreground = totalWatt > 100 ? new SolidColorBrush(Color.FromRgb(250, 204, 21)) : Brushes.White;

                hudTxtCpuTemp.Text = string.Format("{0:F0}°", cpuTemp ?? 0);
                hudTxtCpuTemp.Foreground = GetTempBrush(cpuTemp ?? 0, Config.CpuTempOrange, Config.CpuTempRed);

                hudTxtGpuTemp.Text = string.Format("{0:F0}°", gpuTemp ?? 0);
                hudTxtGpuTemp.Foreground = GetTempBrush(gpuTemp ?? 0, Config.GpuTempOrange, Config.GpuTempRed);

                hudTxtCpuWatt.Text = string.Format("{0:F0}W", cpuWatt);
                hudTxtGpuWatt.Text = string.Format("{0:F0}W", gpuWatt);

                hudTxtKwh.Text = string.Format(CultureInfo.InvariantCulture, "{0:F4} kWh", accumulatedKwh);
                hudTxtCost.Text = string.Format("Rp {0:N2}", sessionCost);
                return;
            }

            // 2. UPDATE FULL DASHBOARD
            TimeSpan duration = DateTime.Now - sessionStartTime;
            fullTxtSessionDuration.Text = string.Format(" · {0:D2}:{1:D2}:{2:D2}", (int)duration.TotalHours, duration.Minutes, duration.Seconds);

            fullTxtTotalWatt.Text = string.Format("{0:F1}", totalWatt);
            fullTxtTotalWatt.Foreground = totalWatt > 120 ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) : (totalWatt > 80 ? new SolidColorBrush(Color.FromRgb(245, 158, 11)) : Brushes.White);
            fullTxtAvgWatt.Text = string.Format("{0:F1}", avgWatt);

            fullTxtSessionKwh.Text = string.Format(CultureInfo.InvariantCulture, " ({0:F4} kWh)", accumulatedKwh);
            fullTxtSessionCost.Text = string.Format("Rp {0:N2}", sessionCost);

            // CPU Stats
            fullTxtCpuTemp.Text = cpuTemp.HasValue ? string.Format("{0:F0}°C", cpuTemp.Value) : "--°C";
            fullTxtCpuTemp.Foreground = GetTempBrush(cpuTemp ?? 0, Config.CpuTempOrange, Config.CpuTempRed);
            fullTxtCpuWatt.Text = string.Format("{0:F1} W", cpuWatt);
            fullPbCpuWatt.Value = Math.Min(150, cpuWatt);

            // GPU Stats
            fullTxtGpuTemp.Text = gpuTemp.HasValue ? string.Format("{0:F0}°C", gpuTemp.Value) : "--°C";
            fullTxtGpuTemp.Foreground = GetTempBrush(gpuTemp ?? 0, Config.GpuTempOrange, Config.GpuTempRed);
            fullTxtGpuWatt.Text = string.Format("{0:F1} W", gpuWatt);
            fullPbGpuWatt.Value = Math.Min(250, gpuWatt);

            // Monitor Stats (Page 1)
            string monitorKey = null;
            double monitorWatt = 0;
            foreach (var st in Config.StaticPeripherals)
            {
                string lower = st.Key.ToLower();
                if (lower.Contains("monitor") || lower.Contains("display") || lower.Contains("screen") ||
                    lower.Contains("lg") || lower.Contains("samsung") || lower.Contains("asus") ||
                    lower.Contains("dell") || lower.Contains("benq") || lower.Contains("acer") ||
                    lower.Contains("viewsonic") || lower.Contains("aoc"))
                {
                    monitorKey = st.Key;
                    monitorWatt = st.Value;
                    break;
                }
            }
            if (monitorKey == null && Config.StaticPeripherals.Count > 0)
            {
                var enumerator = Config.StaticPeripherals.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    monitorKey = enumerator.Current.Key;
                    monitorWatt = enumerator.Current.Value;
                }
            }

            if (monitorKey != null)
            {
                fullTxtMonitorName.Text = " " + monitorKey;
                fullTxtMonitorWatt.Text = string.Format("{0:F1} W", monitorWatt);
                fullPbMonitorWatt.Value = Math.Min(100, monitorWatt);
            }
            else
            {
                fullTxtMonitorName.Text = " Layar / Monitor";
                fullTxtMonitorWatt.Text = "0.0 W";
                fullPbMonitorWatt.Value = 0;
            }

            // Projections (Page 2)
            fullTxtCostPerHour.Text = string.Format("Rp {0:N0}", costPerHour);
            fullTxtCostPerDay.Text = string.Format("Rp {0:N0}", costPerDay);
            fullTxtCostPerMonth.Text = string.Format("Rp {0:N0}", costPerMonth);

            // Hardware List (Page 2: Komponen & sensor sekunder lainnya)
            fullHwSensorsContainer.Children.Clear();
            foreach (var s in sensors)
            {
                if (s.Type == HardwareType.Cpu || s.Type == HardwareType.GpuNvidia || s.Type == HardwareType.GpuAmd || s.Type == HardwareType.GpuIntel)
                    continue;

                fullHwSensorsContainer.Children.Add(CreateHardwareSensorRow(s.Name, s.Watt, s.Temp));
            }
            foreach (var st in Config.StaticPeripherals)
            {
                if (monitorKey != null && st.Key == monitorKey)
                    continue;

                fullHwSensorsContainer.Children.Add(CreateHardwareSensorRow(st.Key, st.Value, null, true));
            }

            if (fullHwSensorsContainer.Children.Count == 0)
            {
                TextBlock emptyTxt = new TextBlock
                {
                    Text = "Tidak ada komponen sekunder.",
                    FontSize = 11,
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 10, 0, 10),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                fullHwSensorsContainer.Children.Add(emptyTxt);
            }
        }

        private Grid CreateHardwareSensorRow(string name, double watt, double? temp, bool isStatic = false)
        {
            Grid g = new Grid { Margin = new Thickness(0, 2.5, 0, 2.5) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock txtName = new TextBlock
            {
                Text = name,
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = isStatic ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(txtName, 0);
            g.Children.Add(txtName);

            string valStr = string.Format("{0,5:F1} W", watt);
            if (temp.HasValue) valStr += string.Format(" · {0:F0}°C", temp.Value);

            TextBlock txtVal = new TextBlock
            {
                Text = valStr,
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(txtVal, 1);
            g.Children.Add(txtVal);

            return g;
        }

        private Brush GetTempBrush(double temp, double orangeThresh, double redThresh)
        {
            if (temp >= redThresh) return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            if (temp >= orangeThresh) return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
            return new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate-400
        }

        protected override void OnClosed(EventArgs e)
        {
            AppLogger.Info(string.Format("WPF Window closed | kWh={0:F4} | Rp={1:N2}", accumulatedKwh, accumulatedKwh * Config.Tariff));
            double deltaHours = activeRunningHours - lastCommittedHours;
            double deltaKwh = accumulatedKwh - lastCommittedKwh;
            var deltaHw = GetHardwareDeltas();
            if (deltaHours > 0 || deltaKwh > 0 || deltaHw.Count > 0)
            {
                SessionManager.WriteSessionLog(sessionStartTime, deltaHours * 60.0, deltaKwh, deltaKwh * Config.Tariff, deltaHw);
            }
            if (reportViewerWindow != null)
            {
                try { reportViewerWindow.Close(); } catch { }
                reportViewerWindow = null;
            }
            if (settingsWindow != null)
            {
                try { settingsWindow.Close(); } catch { }
                settingsWindow = null;
            }
            if (updateTimer != null) updateTimer.Stop();
            if (autosaveTimer != null) autosaveTimer.Stop();
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            if (hardwareScanner != null) hardwareScanner.Close();
            AppLogger.LogShutdown("OnWpfWindowClosed");
            base.OnClosed(e);
        }
    }
}
