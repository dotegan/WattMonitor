using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WattMonitor
{
    public class SettingsWindow : Window
    {
        private Action onSettingsApplied;
        private int currentTab = 0;

        // Tabs
        private Border tabBtnGeneral;
        private Border tabBtnDisplay;
        private Border tabBtnThreshold;
        private Border tabBtnComponent;

        private TextBlock tabTxtGeneral;
        private TextBlock tabTxtDisplay;
        private TextBlock tabTxtThreshold;
        private TextBlock tabTxtComponent;

        // Scroll Containers
        private ScrollViewer scrollGeneral;
        private ScrollViewer scrollDisplay;
        private ScrollViewer scrollThreshold;
        private ScrollViewer scrollComponent;

        // General Tab Inputs
        private TextBox tbTariff;
        private TextBox tbHoursPerDay;
        private TextBox tbAutosaveMinutes;
        private TextBox tbWattOffset;

        // Display Tab Inputs
        private Slider sliderRefreshRate;
        private TextBlock txtRefreshRateVal;
        private Slider sliderOpacity;
        private TextBlock txtOpacityVal;
        private Slider sliderFontSize;
        private TextBlock txtFontSizeVal;
        private Slider sliderMinOpacity;
        private TextBlock txtMinOpacityVal;

        // Threshold Tab Inputs
        private TextBox tbCpuTempOrange;
        private TextBox tbCpuTempRed;
        private TextBox tbCpuWattOrange;
        private TextBox tbCpuWattRed;
        private TextBox tbGpuTempOrange;
        private TextBox tbGpuTempRed;
        private TextBox tbGpuWattOrange;
        private TextBox tbGpuWattRed;

        // Component Tab Controls
        private StackPanel spComponentList;
        private TextBlock txtComponentCount;
        private TextBox tbNewCompName;
        private TextBox tbNewCompWatt;
        private List<HardwareComponentConfig> workingComponents;
        private List<Tuple<HardwareComponentConfig, TextBox>> wattInputBindings;

        // Footer
        private TextBlock txtStatusMsg;

        public SettingsWindow(Action onApplied = null, int initialTab = 0)
        {
            this.Title = "Pengaturan - WattMonitor";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 480;
            this.Height = 600;
            this.MinWidth = 440;
            this.MinHeight = 500;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            this.onSettingsApplied = onApplied;
            this.workingComponents = new List<HardwareComponentConfig>();
            foreach (var c in Config.Components)
            {
                this.workingComponents.Add(new HardwareComponentConfig
                {
                    Name = c.Name,
                    Category = c.Category,
                    IsEnabled = c.IsEnabled,
                    EstimatedWatt = c.EstimatedWatt,
                    IsLiveSensor = c.IsLiveSensor,
                    IsCustom = c.IsCustom
                });
            }
            this.wattInputBindings = new List<Tuple<HardwareComponentConfig, TextBox>>();

            ModernDialog.ApplyDarkToolTipStyle(this);
            ApplyModernScrollbarStyle();
            ApplyModernCheckBoxStyle();
            BuildUI();
            LoadCurrentValues();
            SelectTab(initialTab);
        }

        private void ApplyModernScrollbarStyle()
        {
            try
            {
                string scrollBarStyleXaml = @"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='{x:Type ScrollBar}'>
    <Setter Property='Stylus.IsPressAndHoldEnabled' Value='false'/>
    <Setter Property='Stylus.IsFlicksEnabled' Value='false'/>
    <Setter Property='Width' Value='6'/>
    <Setter Property='MinWidth' Value='6'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='{x:Type ScrollBar}'>
                <Grid Background='Transparent' SnapsToDevicePixels='true'>
                    <Track x:Name='PART_Track' IsDirectionReversed='true'>
                        <Track.DecreaseRepeatButton>
                            <RepeatButton Command='{x:Static ScrollBar.PageUpCommand}' Opacity='0' Focusable='false'/>
                        </Track.DecreaseRepeatButton>
                        <Track.IncreaseRepeatButton>
                            <RepeatButton Command='{x:Static ScrollBar.PageDownCommand}' Opacity='0' Focusable='false'/>
                        </Track.IncreaseRepeatButton>
                        <Track.Thumb>
                            <Thumb>
                                <Thumb.Template>
                                    <ControlTemplate TargetType='{x:Type Thumb}'>
                                        <Border x:Name='thumbBorder'
                                                CornerRadius='3'
                                                Background='#5A94A3B8'
                                                Margin='1,0,1,0'/>
                                        <ControlTemplate.Triggers>
                                            <Trigger Property='IsMouseOver' Value='true'>
                                                <Setter TargetName='thumbBorder' Property='Background' Value='#B494A3B8'/>
                                            </Trigger>
                                            <Trigger Property='IsDragging' Value='true'>
                                                <Setter TargetName='thumbBorder' Property='Background' Value='#FF38BDF8'/>
                                            </Trigger>
                                        </ControlTemplate.Triggers>
                                    </ControlTemplate>
                                </Thumb.Template>
                            </Thumb>
                        </Track.Thumb>
                    </Track>
                </Grid>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";

                this.Resources[typeof(ScrollBar)] = (Style)System.Windows.Markup.XamlReader.Parse(scrollBarStyleXaml);
            }
            catch { }
        }

        private void ApplyModernCheckBoxStyle()
        {
            try
            {
                string checkBoxStyleXaml = @"
<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       TargetType='{x:Type CheckBox}'>
    <Setter Property='FocusVisualStyle' Value='{x:Null}'/>
    <Setter Property='Cursor' Value='Hand'/>
    <Setter Property='VerticalAlignment' Value='Center'/>
    <Setter Property='HorizontalAlignment' Value='Center'/>
    <Setter Property='SnapsToDevicePixels' Value='true'/>
    <Setter Property='Template'>
        <Setter.Value>
            <ControlTemplate TargetType='{x:Type CheckBox}'>
                <Border x:Name='chkBox'
                        Width='16'
                        Height='16'
                        CornerRadius='4'
                        Background='#FF0F172A'
                        BorderBrush='#FF334155'
                        BorderThickness='1'
                        Padding='3'
                        SnapsToDevicePixels='true'>
                    <Border x:Name='chkIndicator'
                            CornerRadius='2'
                            Background='#FF38BDF8'
                            Visibility='Collapsed'
                            SnapsToDevicePixels='true'/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property='IsChecked' Value='true'>
                        <Setter TargetName='chkBox' Property='BorderBrush' Value='#FF0284C7'/>
                        <Setter TargetName='chkIndicator' Property='Visibility' Value='Visible'/>
                    </Trigger>
                    <Trigger Property='IsMouseOver' Value='true'>
                        <Setter TargetName='chkBox' Property='BorderBrush' Value='#FF475569'/>
                    </Trigger>
                    <MultiTrigger>
                        <MultiTrigger.Conditions>
                            <Condition Property='IsMouseOver' Value='true'/>
                            <Condition Property='IsChecked' Value='true'/>
                        </MultiTrigger.Conditions>
                        <Setter TargetName='chkBox' Property='BorderBrush' Value='#FF38BDF8'/>
                        <Setter TargetName='chkIndicator' Property='Background' Value='#FF0284C7'/>
                    </MultiTrigger>
                    <Trigger Property='IsEnabled' Value='false'>
                        <Setter TargetName='chkBox' Property='Opacity' Value='0.35'/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
        </Setter.Value>
    </Setter>
</Style>";

                this.Resources[typeof(CheckBox)] = (Style)System.Windows.Markup.XamlReader.Parse(checkBoxStyleXaml);
            }
            catch { }
        }

        private void BuildUI()
        {
            // Root Card
            Border rootCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 15, 23, 42)), // Slate 900
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate 700
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(12)
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Tabs
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Footer

            // ================= 1. WINDOW HEADER =================
            Border headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 12, 12, 12),
                CornerRadius = new CornerRadius(12, 12, 0, 0)
            };
            headerBorder.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconSettings = LucideIcon.Create(LucideIcon.Settings, 16, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false, 2.0);
            iconSettings.Margin = new Thickness(0, 0, 8, 0);
            titleSp.Children.Add(iconSettings);

            TextBlock titleTxt = new TextBlock
            {
                Text = "Pengaturan WattMonitor",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(titleTxt);
            Grid.SetColumn(titleSp, 0);
            headerGrid.Children.Add(titleSp);

            StackPanel actSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            actSp.Children.Add(CreateHeaderButton(LucideIcon.Minimize, "Minimize", () => this.WindowState = WindowState.Minimized));
            actSp.Children.Add(CreateHeaderButton(LucideIcon.X, "Tutup", () => this.Close()));
            Grid.SetColumn(actSp, 1);
            headerGrid.Children.Add(actSp);

            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // ================= 2. TAB BAR (4 Pills Container) =================
            Border tabBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 8, 16, 8)
            };

            Border tabPillContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 2, 6, 23)), // Slate 950
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(3)
            };

            Grid tabGrid = new Grid();
            for (int i = 0; i < 4; i++)
            {
                tabGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            tabBtnGeneral = CreateTabPillButton("Tarif", LucideIcon.Zap, 0, out tabTxtGeneral);
            Grid.SetColumn(tabBtnGeneral, 0);
            tabGrid.Children.Add(tabBtnGeneral);

            tabBtnDisplay = CreateTabPillButton("Layar", LucideIcon.Monitor, 1, out tabTxtDisplay);
            Grid.SetColumn(tabBtnDisplay, 1);
            tabGrid.Children.Add(tabBtnDisplay);

            tabBtnThreshold = CreateTabPillButton("Batas", LucideIcon.Thermometer, 2, out tabTxtThreshold);
            Grid.SetColumn(tabBtnThreshold, 2);
            tabGrid.Children.Add(tabBtnThreshold);

            tabBtnComponent = CreateTabPillButton("Komponen", LucideIcon.Cpu, 3, out tabTxtComponent);
            Grid.SetColumn(tabBtnComponent, 3);
            tabGrid.Children.Add(tabBtnComponent);

            tabPillContainer.Child = tabGrid;
            tabBar.Child = tabPillContainer;
            Grid.SetRow(tabBar, 1);
            mainGrid.Children.Add(tabBar);

            // ================= 3. CONTENT AREA =================
            Grid contentGrid = new Grid { Margin = new Thickness(16, 12, 16, 12) };

            scrollGeneral = new ScrollViewer { Content = BuildGeneralTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            contentGrid.Children.Add(scrollGeneral);

            scrollDisplay = new ScrollViewer { Content = BuildDisplayTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollDisplay);

            scrollThreshold = new ScrollViewer { Content = BuildThresholdTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollThreshold);

            scrollComponent = new ScrollViewer { Content = BuildComponentTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollComponent);

            Grid.SetRow(contentGrid, 2);
            mainGrid.Children.Add(contentGrid);

            // ================= 4. FOOTER =================
            Border footerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 10, 16, 10),
                CornerRadius = new CornerRadius(0, 0, 10, 10)
            };

            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Status message
            txtStatusMsg = new TextBlock
            {
                Text = "Siap.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                FontFamily = new FontFamily("Segoe UI, Inter"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtStatusMsg, 0);
            footerGrid.Children.Add(txtStatusMsg);

            // Action Buttons
            StackPanel btnSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button btnReset = CreateFooterButton("Reset", LucideIcon.RotateCcw, "Reset semua ke konfigurasi default", ResetToDefaults, false, false);
            btnSp.Children.Add(btnReset);

            Button btnCancel = CreateFooterButton("Batal", LucideIcon.X, "Tutup tanpa menyimpan", () => this.Close(), false, false);
            btnSp.Children.Add(btnCancel);

            Button btnSave = CreateFooterButton("Simpan", LucideIcon.Save, "Simpan perubahan dan terapkan ke aplikasi", SaveAndApplySettings, true, false);
            btnSp.Children.Add(btnSave);

            Grid.SetColumn(btnSp, 1);
            footerGrid.Children.Add(btnSp);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 3);
            mainGrid.Children.Add(footerBorder);

            rootCard.Child = mainGrid;
            this.Content = rootCard;
        }

        // ================= TAB 1: UMUM & TARIF =================
        private UIElement BuildGeneralTab()
        {
            StackPanel sp = new StackPanel();

            Border cardTariff = CreateSectionCard("Kalkulasi Tarif & Listrik", "Formula kalkulasi estimasi pengeluaran biaya listrik.");
            StackPanel cardSp = new StackPanel();

            tbTariff = CreateInputFieldWithSuffix("Tarif Dasar Listrik", "Rp/kWh", "Contoh: 1444.70", cardSp);
            tbHoursPerDay = CreateInputFieldWithSuffix("Estimasi Jam Aktif", "Jam/hari", "Digunakan untuk estimasi bulanan (cth: 12)", cardSp);
            tbAutosaveMinutes = CreateInputFieldWithSuffix("Interval Simpan Data", "Menit", "0.08 = ~5 detik proteksi mati lampu", cardSp);
            tbWattOffset = CreateInputFieldWithSuffix("Kompensasi Standby", "Watt", "Tambahan daya tetap di luar sensor hardware", cardSp);

            cardTariff.Child = cardSp;
            sp.Children.Add(cardTariff);

            return sp;
        }

        // ================= TAB 2: TAMPILAN & OVERLAY =================
        private UIElement BuildDisplayTab()
        {
            StackPanel sp = new StackPanel();

            Border cardDisplay = CreateSectionCard("Tampilan & Overlay", "Frekuensi pembaruan sensor, tingkat transparansi, dan font HUD.");
            StackPanel cardSp = new StackPanel();

            cardSp.Children.Add(CreateSliderRow("Refresh Rate Sensor", out sliderRefreshRate, out txtRefreshRateVal, 100, 2000, 100, (v) => string.Format("{0} ms", (int)v)));
            cardSp.Children.Add(CreateSliderRow("Transparansi Window Utama", out sliderOpacity, out txtOpacityVal, 0.2, 1.0, 0.05, (v) => string.Format("{0}%", (int)(v * 100))));
            cardSp.Children.Add(CreateSliderRow("Ukuran Font Minimalis", out sliderFontSize, out txtFontSizeVal, 8, 18, 1, (v) => string.Format("{0} pt", (int)v)));
            cardSp.Children.Add(CreateSliderRow("Transparansi Minimalis", out sliderMinOpacity, out txtMinOpacityVal, 0.2, 1.0, 0.05, (v) => string.Format("{0}%", (int)(v * 100))));

            cardDisplay.Child = cardSp;
            sp.Children.Add(cardDisplay);

            return sp;
        }

        private UIElement CreateSliderRow(string label, out Slider slider, out TextBlock txtVal, double min, double max, double tick, Func<double, string> formatVal)
        {
            StackPanel rowSp = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            TextBlock lbl = new TextBlock
            {
                Text = label,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            rowSp.Children.Add(lbl);

            Grid sliderGrid = new Grid();
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                TickFrequency = tick,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 22
            };
            Grid.SetColumn(slider, 0);
            sliderGrid.Children.Add(slider);

            Border badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                MinWidth = 54
            };

            txtVal = new TextBlock
            {
                Text = "",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), // Sky 400
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = txtVal;
            Grid.SetColumn(badge, 2);
            sliderGrid.Children.Add(badge);

            TextBlock capturedTxt = txtVal;
            slider.ValueChanged += (s, e) =>
            {
                capturedTxt.Text = formatVal(e.NewValue);
            };

            rowSp.Children.Add(sliderGrid);
            return rowSp;
        }

        // ================= TAB 3: BATAS SUHU & DAYA =================
        private UIElement BuildThresholdTab()
        {
            StackPanel sp = new StackPanel();

            // CPU Thresholds Card
            Border cardCpu = CreateSectionCard("Ambang Batas CPU", "Warna indikator CPU berubah menjadi Orange (waspada) dan Merah (kritis).");
            StackPanel spCpu = new StackPanel();

            Grid gCpu = new Grid { Margin = new Thickness(0, 2, 0, 4) };
            gCpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gCpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            gCpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel spCpuLeft = new StackPanel();
            tbCpuTempOrange = CreateInputFieldWithSuffix("Suhu Kuning/Oranye", "°C", "55", spCpuLeft);
            tbCpuWattOrange = CreateInputFieldWithSuffix("Daya Kuning/Oranye", "W", "45", spCpuLeft);
            Grid.SetColumn(spCpuLeft, 0);
            gCpu.Children.Add(spCpuLeft);

            StackPanel spCpuRight = new StackPanel();
            tbCpuTempRed = CreateInputFieldWithSuffix("Suhu Merah", "°C", "68", spCpuRight);
            tbCpuWattRed = CreateInputFieldWithSuffix("Daya Merah", "W", "60", spCpuRight);
            Grid.SetColumn(spCpuRight, 2);
            gCpu.Children.Add(spCpuRight);

            spCpu.Children.Add(gCpu);
            cardCpu.Child = spCpu;
            sp.Children.Add(cardCpu);

            // GPU Thresholds Card
            Border cardGpu = CreateSectionCard("Ambang Batas GPU", "Warna indikator GPU berubah menjadi Orange (waspada) dan Merah (kritis).");
            StackPanel spGpu = new StackPanel();

            Grid gGpu = new Grid { Margin = new Thickness(0, 2, 0, 4) };
            gGpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gGpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            gGpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel spGpuLeft = new StackPanel();
            tbGpuTempOrange = CreateInputFieldWithSuffix("Suhu Kuning/Oranye", "°C", "60", spGpuLeft);
            tbGpuWattOrange = CreateInputFieldWithSuffix("Daya Kuning/Oranye", "W", "95", spGpuLeft);
            Grid.SetColumn(spGpuLeft, 0);
            gGpu.Children.Add(spGpuLeft);

            StackPanel spGpuRight = new StackPanel();
            tbGpuTempRed = CreateInputFieldWithSuffix("Suhu Merah", "°C", "72", spGpuRight);
            tbGpuWattRed = CreateInputFieldWithSuffix("Daya Merah", "W", "130", spGpuRight);
            Grid.SetColumn(spGpuRight, 2);
            gGpu.Children.Add(spGpuRight);

            spGpu.Children.Add(gGpu);
            cardGpu.Child = spGpu;
            sp.Children.Add(cardGpu);

            return sp;
        }

        // ================= TAB 4: DETEKSI KOMPONEN (AUTODETECT & WATT) =================
        private UIElement BuildComponentTab()
        {
            StackPanel sp = new StackPanel();

            // Header Section Card
            Border cardHeader = CreateSectionCard("Deteksi Komponen & Estimasi Daya", "Pilih komponen yang ingin dihitung dayanya. Watt CPU & GPU dibaca real-time, komponen lain menggunakan estimasi.");
            StackPanel headerSp = (StackPanel)cardHeader.Child;

            // Action Row: Rescan Button & Counter
            Grid actGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button btnRescan = CreateActionButton(LucideIcon.RotateCcw, "Pindai Ulang Hardware (Rescan)", () =>
            {
                SyncWattInputs();
                var detected = HardwareDetector.DetectAll();
                workingComponents = HardwareDetector.MergeWithExisting(workingComponents, detected);
                RenderComponentList();
                txtStatusMsg.Text = "Hardware berhasil dipindai ulang.";
            }, Color.FromArgb(200, 30, 41, 59), Color.FromRgb(56, 189, 248));
            btnRescan.Width = 32;
            btnRescan.Height = 28;
            Grid.SetColumn(btnRescan, 0);
            actGrid.Children.Add(btnRescan);

            txtComponentCount = new TextBlock
            {
                Text = "",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                FontFamily = new FontFamily("Segoe UI, Inter"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtComponentCount, 1);
            actGrid.Children.Add(txtComponentCount);

            headerSp.Children.Add(actGrid);

            // Container daftar komponen
            spComponentList = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            headerSp.Children.Add(spComponentList);

            // Card Tambah Komponen Manual
            Border addBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 8, 0, 0)
            };

            StackPanel addSp = new StackPanel();
            TextBlock addTitle = new TextBlock
            {
                Text = "Tambah Periferal Manual (Opsional):",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            addSp.Children.Add(addTitle);

            Grid addGrid = new Grid();
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid nameWrap = CreatePlaceholderTextBox("Nama periferal (cth: Fan Casing)", out tbNewCompName);
            Grid.SetColumn(nameWrap, 0);
            addGrid.Children.Add(nameWrap);

            Grid wattWrap = CreatePlaceholderTextBox("Watt (cth: 15)", out tbNewCompWatt);
            Grid.SetColumn(wattWrap, 2);
            addGrid.Children.Add(wattWrap);

            Button btnAdd = CreateActionButton(LucideIcon.Plus, "Tambah Periferal Manual", () =>
            {
                string name = tbNewCompName.Text.Trim();
                double w;
                if (!string.IsNullOrEmpty(name) && double.TryParse(tbNewCompWatt.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out w))
                {
                    SyncWattInputs();
                    workingComponents.Add(new HardwareComponentConfig(name, "Custom", false, w, true));
                    tbNewCompName.Text = "";
                    tbNewCompWatt.Text = "";
                    RenderComponentList();
                }
                else
                {
                    ModernDialog.Show("Input Tidak Valid", "Mohon masukkan nama perangkat dan angka watt yang valid.", this, true);
                }
            }, Color.FromArgb(220, 2, 132, 199), Color.FromRgb(255, 255, 255));
            btnAdd.Width = 32;
            btnAdd.Height = 28;
            Grid.SetColumn(btnAdd, 4);
            addGrid.Children.Add(btnAdd);

            addSp.Children.Add(addGrid);
            addBox.Child = addSp;
            headerSp.Children.Add(addBox);

            sp.Children.Add(cardHeader);

            RenderComponentList();
            return sp;
        }

        private void SyncWattInputs()
        {
            foreach (var tuple in wattInputBindings)
            {
                var comp = tuple.Item1;
                var tb = tuple.Item2;
                if (!comp.IsLiveSensor && tb != null)
                {
                    double w;
                    if (double.TryParse(tb.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out w))
                    {
                        comp.EstimatedWatt = Math.Max(0, w);
                    }
                }
            }
        }

        private void UpdateComponentCountBadge()
        {
            if (txtComponentCount == null) return;
            int total = workingComponents.Count;
            int enabled = 0;
            foreach (var c in workingComponents) if (c.IsEnabled) enabled++;
            txtComponentCount.Text = string.Format("{0} dari {1} komponen aktif", enabled, total);
        }

        private void RenderComponentList()
        {
            if (spComponentList == null) return;
            spComponentList.Children.Clear();
            wattInputBindings.Clear();

            if (workingComponents.Count == 0)
            {
                Border emptyBox = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(80, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 12, 12, 12),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                emptyBox.Child = new TextBlock
                {
                    Text = "Belum ada hardware terdeteksi. Silakan klik tombol 'Pindai Ulang Hardware'.",
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spComponentList.Children.Add(emptyBox);
                UpdateComponentCountBadge();
                return;
            }

            var liveList = new List<HardwareComponentConfig>();
            var staticList = new List<HardwareComponentConfig>();

            foreach (var item in workingComponents)
            {
                if (item.IsLiveSensor) liveList.Add(item);
                else staticList.Add(item);
            }

            // ================= 1. LIVE POWER SENSORS =================
            if (liveList.Count > 0)
            {
                spComponentList.Children.Add(CreateGroupSectionHeader(
                    "SENSOR DINAMIS REAL-TIME (LIVE)",
                    "Daya listrik (Watt) dibaca langsung dari chip sensor internal.",
                    LucideIcon.Zap,
                    Color.FromRgb(52, 211, 153)
                ));

                foreach (var item in liveList)
                {
                    spComponentList.Children.Add(CreateComponentRow(item));
                }
            }

            // ================= 2. ESTIMATED HARDWARE & PERIPHERALS =================
            if (staticList.Count > 0)
            {
                Border staticHeader = CreateGroupSectionHeader(
                    "ESTIMASI DAYA KOMPONEN & PERIFERAL",
                    "Daya dihitung menggunakan estimasi baseline TDP atau watt manual.",
                    LucideIcon.Settings,
                    Color.FromRgb(56, 189, 248)
                );
                if (liveList.Count > 0) staticHeader.Margin = new Thickness(0, 12, 0, 6);
                spComponentList.Children.Add(staticHeader);

                foreach (var item in staticList)
                {
                    spComponentList.Children.Add(CreateComponentRow(item));
                }
            }

            UpdateComponentCountBadge();
        }

        private Border CreateGroupSectionHeader(string title, string subtitle, string iconPath, Color accentColor)
        {
            Border b = new Border
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 4, 0, 6)
            };

            StackPanel sp = new StackPanel();

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var icon = LucideIcon.Create(iconPath, 12, new SolidColorBrush(accentColor), false, 2.0);
            icon.Margin = new Thickness(0, 0, 6, 0);
            titleSp.Children.Add(icon);

            TextBlock tbTitle = new TextBlock
            {
                Text = title,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accentColor),
                FontFamily = new FontFamily("Segoe UI, Inter"),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(tbTitle);
            sp.Children.Add(titleSp);

            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock tbSub = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Margin = new Thickness(18, 1, 0, 0)
                };
                sp.Children.Add(tbSub);
            }

            b.Child = sp;
            return b;
        }

        private Border CreateComponentRow(HardwareComponentConfig c)
        {
            Border row = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 5)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) }); // Col 0: CheckBox
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58, GridUnitType.Pixel) }); // Col 1: Category Badge (52px + 6px margin)
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Col 2: Name (Ellipsis)
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(106, GridUnitType.Pixel) }); // Col 3: Right Value / Watt Box

            // Col 0: Checkbox (Centered in 28px)
            CheckBox chk = new CheckBox
            {
                IsChecked = c.IsEnabled,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Cursor = Cursors.Hand
            };
            Grid.SetColumn(chk, 0);
            g.Children.Add(chk);

            // Col 1: Category Badge (Exact 52px width)
            Border catBadge = CreateCategoryBadge(c.Category);
            catBadge.Margin = new Thickness(0, 0, 6, 0);
            Grid.SetColumn(catBadge, 1);
            g.Children.Add(catBadge);

            // Col 2: Component Name (Ellipsis on overflow)
            TextBlock txtName = new TextBlock
            {
                Text = c.Name,
                FontSize = 11.5,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = c.Name,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(txtName, 2);
            g.Children.Add(txtName);

            // Col 3: Right Value Box (Exact 106px width)
            TextBox tbWatt = null;
            if (c.IsLiveSensor)
            {
                Border livePill = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(35, 16, 185, 129)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 16, 185, 129)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Width = 104,
                    Height = 26,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                StackPanel pillSp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                pillSp.Children.Add(LucideIcon.Create(LucideIcon.Zap, 11, new SolidColorBrush(Color.FromRgb(52, 211, 153)), false, 2.0));
                TextBlock txtLive = new TextBlock
                {
                    Text = "Live Sensor",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)),
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                pillSp.Children.Add(txtLive);
                livePill.Child = pillSp;
                Grid.SetColumn(livePill, 3);
                g.Children.Add(livePill);
            }
            else
            {
                Grid wattGrid = new Grid
                {
                    Width = 104,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                wattGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52, GridUnitType.Pixel) }); // tbWatt
                wattGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24, GridUnitType.Pixel) }); // " W"
                wattGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28, GridUnitType.Pixel) }); // Delete button or spacer

                tbWatt = new TextBox
                {
                    Text = c.EstimatedWatt.ToString("0.##", CultureInfo.InvariantCulture),
                    Width = 52,
                    Height = 26,
                    Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                    BorderThickness = new Thickness(1),
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Consolas, Segoe UI"),
                    FontSize = 11,
                    Padding = new Thickness(4, 2, 4, 2),
                    TextAlignment = TextAlignment.Right,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    IsEnabled = c.IsEnabled,
                    Opacity = c.IsEnabled ? 1.0 : 0.45
                };
                Grid.SetColumn(tbWatt, 0);
                wattGrid.Children.Add(tbWatt);

                TextBlock txtUnit = new TextBlock
                {
                    Text = " W",
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Left
                };
                Grid.SetColumn(txtUnit, 1);
                wattGrid.Children.Add(txtUnit);

                if (c.IsCustom)
                {
                    Button btnDel = CreateActionButton(LucideIcon.Trash, "Hapus periferal", () =>
                    {
                        SyncWattInputs();
                        workingComponents.Remove(c);
                        RenderComponentList();
                    }, Color.FromArgb(50, 239, 68, 68), Color.FromRgb(248, 113, 113));
                    btnDel.Width = 24;
                    btnDel.Height = 24;
                    btnDel.Margin = new Thickness(4, 0, 0, 0);
                    Grid.SetColumn(btnDel, 2);
                    wattGrid.Children.Add(btnDel);
                }

                Grid.SetColumn(wattGrid, 3);
                g.Children.Add(wattGrid);

                wattInputBindings.Add(new Tuple<HardwareComponentConfig, TextBox>(c, tbWatt));
            }

            // CheckBox change handler
            TextBox capturedTbWatt = tbWatt;
            chk.Click += (s, e) =>
            {
                c.IsEnabled = chk.IsChecked ?? false;
                if (capturedTbWatt != null)
                {
                    capturedTbWatt.IsEnabled = c.IsEnabled;
                    capturedTbWatt.Opacity = c.IsEnabled ? 1.0 : 0.45;
                }
                UpdateComponentCountBadge();
            };

            row.Child = g;
            return row;
        }

        private Border CreateCategoryBadge(string category)
        {
            Color bg, fg;
            string label;

            switch ((category ?? "").ToLower())
            {
                case "cpu":
                    bg = Color.FromArgb(40, 56, 189, 248);
                    fg = Color.FromRgb(56, 189, 248);
                    label = "CPU";
                    break;
                case "gpu":
                    bg = Color.FromArgb(40, 168, 85, 247);
                    fg = Color.FromRgb(192, 132, 252);
                    label = "GPU";
                    break;
                case "motherboard":
                    bg = Color.FromArgb(40, 99, 102, 241);
                    fg = Color.FromRgb(129, 140, 248);
                    label = "Mobo";
                    break;
                case "ram":
                    bg = Color.FromArgb(40, 34, 197, 94);
                    fg = Color.FromRgb(74, 222, 128);
                    label = "RAM";
                    break;
                case "storage":
                    bg = Color.FromArgb(40, 245, 158, 11);
                    fg = Color.FromRgb(251, 191, 36);
                    label = "Disk";
                    break;
                case "display":
                    bg = Color.FromArgb(40, 236, 72, 153);
                    fg = Color.FromRgb(244, 114, 182);
                    label = "Layar";
                    break;
                default:
                    bg = Color.FromArgb(40, 100, 116, 139);
                    fg = Color.FromRgb(148, 163, 184);
                    label = "Kustom";
                    break;
            }

            Border b = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, fg.R, fg.G, fg.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Width = 52,
                Height = 22,
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock txt = new TextBlock
            {
                Text = label,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(fg),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = txt;
            return b;
        }

        // ================= LOAD & SAVE =================
        private void LoadCurrentValues()
        {
            tbTariff.Text = Config.Tariff.ToString("0.##", CultureInfo.InvariantCulture);
            tbHoursPerDay.Text = Config.HoursPerDay.ToString("0.##", CultureInfo.InvariantCulture);
            tbAutosaveMinutes.Text = Config.AutosaveMinutes.ToString("0.##", CultureInfo.InvariantCulture);
            tbWattOffset.Text = Config.WattOffset.ToString("0.##", CultureInfo.InvariantCulture);

            sliderRefreshRate.Value = Config.RefreshRateMs;
            sliderOpacity.Value = Config.Opacity;
            sliderFontSize.Value = Config.MinimalistFontSize;
            sliderMinOpacity.Value = Config.MinimalistOpacity;

            tbCpuTempOrange.Text = Config.CpuTempOrange.ToString("0.##", CultureInfo.InvariantCulture);
            tbCpuTempRed.Text = Config.CpuTempRed.ToString("0.##", CultureInfo.InvariantCulture);
            tbCpuWattOrange.Text = Config.CpuWattOrange.ToString("0.##", CultureInfo.InvariantCulture);
            tbCpuWattRed.Text = Config.CpuWattRed.ToString("0.##", CultureInfo.InvariantCulture);

            tbGpuTempOrange.Text = Config.GpuTempOrange.ToString("0.##", CultureInfo.InvariantCulture);
            tbGpuTempRed.Text = Config.GpuTempRed.ToString("0.##", CultureInfo.InvariantCulture);
            tbGpuWattOrange.Text = Config.GpuWattOrange.ToString("0.##", CultureInfo.InvariantCulture);
            tbGpuWattRed.Text = Config.GpuWattRed.ToString("0.##", CultureInfo.InvariantCulture);

            RenderComponentList();
        }

        private void ResetToDefaults()
        {
            if (!ModernDialog.ShowConfirm("Konfirmasi Reset", "Kembalikan semua nilai ke konfigurasi standar default?", this))
                return;

            tbTariff.Text = "1444.70";
            tbHoursPerDay.Text = "10";
            tbAutosaveMinutes.Text = "0.08";
            tbWattOffset.Text = "0";

            sliderRefreshRate.Value = 500;
            sliderOpacity.Value = 0.9;
            sliderFontSize.Value = 11;
            sliderMinOpacity.Value = 0.9;

            tbCpuTempOrange.Text = "55";
            tbCpuTempRed.Text = "68";
            tbCpuWattOrange.Text = "45";
            tbCpuWattRed.Text = "60";

            tbGpuTempOrange.Text = "60";
            tbGpuTempRed.Text = "72";
            tbGpuWattOrange.Text = "95";
            tbGpuWattRed.Text = "130";

            workingComponents = HardwareDetector.DetectAll();
            RenderComponentList();
        }

        private void SaveAndApplySettings()
        {
            try
            {
                double dVal;
                if (double.TryParse(tbTariff.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.Tariff = dVal;
                if (double.TryParse(tbHoursPerDay.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal))
                {
                    Config.HoursPerDay = dVal;
                    Config.HoursPerMonth = dVal * 30.0;
                }
                if (double.TryParse(tbAutosaveMinutes.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.AutosaveMinutes = dVal;
                if (double.TryParse(tbWattOffset.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.WattOffset = dVal;

                Config.RefreshRateMs = (int)sliderRefreshRate.Value;
                Config.Opacity = sliderOpacity.Value;
                Config.MinimalistFontSize = (float)sliderFontSize.Value;
                Config.MinimalistOpacity = sliderMinOpacity.Value;

                if (double.TryParse(tbCpuTempOrange.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.CpuTempOrange = dVal;
                if (double.TryParse(tbCpuTempRed.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.CpuTempRed = dVal;
                if (double.TryParse(tbCpuWattOrange.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.CpuWattOrange = dVal;
                if (double.TryParse(tbCpuWattRed.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.CpuWattRed = dVal;

                if (double.TryParse(tbGpuTempOrange.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.GpuTempOrange = dVal;
                if (double.TryParse(tbGpuTempRed.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.GpuTempRed = dVal;
                if (double.TryParse(tbGpuWattOrange.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.GpuWattOrange = dVal;
                if (double.TryParse(tbGpuWattRed.Text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out dVal)) Config.GpuWattRed = dVal;

                // Sinkronkan nilai watt komponen
                SyncWattInputs();
                Config.Components = new List<HardwareComponentConfig>(workingComponents);

                // Simpan ke config.json
                Config.Save();

                txtStatusMsg.Text = "Pengaturan berhasil disimpan.";

                // Trigger reload pada window utama
                if (onSettingsApplied != null) onSettingsApplied();
            }
            catch (Exception ex)
            {
                ModernDialog.Show("Gagal Menyimpan", "Terjadi kesalahan saat menyimpan pengaturan: " + ex.Message, this, true);
            }
        }

        // ================= HELPERS & STYLES =================
        private Border CreateSectionCard(string title, string subtitle)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel sp = new StackPanel();
            TextBlock tTxt = new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Inter")
            };
            sp.Children.Add(tTxt);

            if (!string.IsNullOrEmpty(subtitle))
            {
                TextBlock sTxt = new TextBlock
                {
                    Text = subtitle,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Margin = new Thickness(0, 2, 0, 12),
                    TextWrapping = TextWrapping.Wrap
                };
                sp.Children.Add(sTxt);
            }

            b.Child = sp;
            return b;
        }

        private TextBox CreateInputFieldWithSuffix(string labelText, string suffix, string hintText, StackPanel parent)
        {
            parent.Children.Add(CreateLabel(labelText));

            Border box = new Border
            {
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate 700
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 6)
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBox tb = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Padding = new Thickness(8, 4, 6, 4),
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = hintText
            };
            Grid.SetColumn(tb, 0);
            g.Children.Add(tb);

            if (!string.IsNullOrEmpty(suffix))
            {
                Border sfxBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(80, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                    BorderThickness = new Thickness(1, 0, 0, 0),
                    Padding = new Thickness(7, 0, 8, 0),
                    CornerRadius = new CornerRadius(0, 4, 4, 0)
                };
                TextBlock sfxTxt = new TextBlock
                {
                    Text = suffix,
                    FontSize = 10,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                sfxBorder.Child = sfxTxt;
                Grid.SetColumn(sfxBorder, 1);
                g.Children.Add(sfxBorder);
            }

            box.Child = g;
            parent.Children.Add(box);
            return tb;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Margin = new Thickness(0, 2, 0, 3)
            };
        }

        private Grid CreatePlaceholderTextBox(string placeholderText, out TextBox tb)
        {
            Grid g = new Grid();

            tb = new TextBox
            {
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                Padding = new Thickness(8, 4, 8, 4),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            TextBlock watermark = new TextBlock
            {
                Text = placeholderText,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)), // Slate 500
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            TextBox capturedTb = tb;
            capturedTb.TextChanged += (s, e) =>
            {
                watermark.Visibility = string.IsNullOrEmpty(capturedTb.Text) ? Visibility.Visible : Visibility.Collapsed;
            };

            g.Children.Add(tb);
            g.Children.Add(watermark);
            return g;
        }

        private Button CreateActionButton(string iconPath, string tooltip, Action onClick, Color? bg = null, Color? fg = null)
        {
            Color bgColor = bg ?? Color.FromArgb(200, 30, 41, 59);
            Color fgColor = fg ?? Color.FromRgb(148, 163, 184);

            Button btn = new Button
            {
                Cursor = Cursors.Hand,
                ToolTip = tooltip,
                Focusable = false
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(bgColor));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(120, 51, 65, 85)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 51, 65, 85)), "bd"));
            hoverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(71, 85, 105)), "bd"));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            btn.Content = LucideIcon.Create(iconPath, 13, new SolidColorBrush(fgColor), false, 1.8);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Border CreateTabPillButton(string title, string iconPath, int tabIndex, out TextBlock txt)
        {
            Border b = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6, 8, 6),
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0)
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var icon = LucideIcon.Create(iconPath, 13, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 1.8);
            icon.Margin = new Thickness(0, 0, 6, 0);
            sp.Children.Add(icon);

            txt = new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
                FontFamily = new FontFamily("Segoe UI, Inter")
            };
            sp.Children.Add(txt);
            b.Child = sp;

            b.MouseDown += (s, e) => SelectTab(tabIndex);
            return b;
        }

        public void SelectTab(int tabIndex)
        {
            currentTab = tabIndex;

            tabBtnGeneral.Background = Brushes.Transparent;
            tabTxtGeneral.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnDisplay.Background = Brushes.Transparent;
            tabTxtDisplay.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnThreshold.Background = Brushes.Transparent;
            tabTxtThreshold.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnComponent.Background = Brushes.Transparent;
            tabTxtComponent.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            scrollGeneral.Visibility = Visibility.Collapsed;
            scrollDisplay.Visibility = Visibility.Collapsed;
            scrollThreshold.Visibility = Visibility.Collapsed;
            scrollComponent.Visibility = Visibility.Collapsed;

            Border activeBtn = null;
            TextBlock activeTxt = null;
            ScrollViewer activeScroll = null;

            switch (tabIndex)
            {
                case 0: activeBtn = tabBtnGeneral; activeTxt = tabTxtGeneral; activeScroll = scrollGeneral; break;
                case 1: activeBtn = tabBtnDisplay; activeTxt = tabTxtDisplay; activeScroll = scrollDisplay; break;
                case 2: activeBtn = tabBtnThreshold; activeTxt = tabTxtThreshold; activeScroll = scrollThreshold; break;
                case 3: activeBtn = tabBtnComponent; activeTxt = tabTxtComponent; activeScroll = scrollComponent; break;
            }

            if (activeBtn != null && activeTxt != null && activeScroll != null)
            {
                activeBtn.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)); // Slate 800
                activeTxt.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Sky 400 active
                activeScroll.Visibility = Visibility.Visible;
            }
        }

        private Button CreateFooterButton(string text, string iconPath, string tooltip, Action onClick, bool isPrimary, bool isDanger)
        {
            Button btn = new Button
            {
                Height = 32,
                Cursor = Cursors.Hand,
                Margin = new Thickness(6, 0, 0, 0),
                ToolTip = tooltip,
                Focusable = false
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

            if (isPrimary)
            {
                border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(2, 132, 199))); // Sky 600
                border.SetValue(Border.BorderThicknessProperty, new Thickness(0));
            }
            else if (isDanger)
            {
                border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(50, 239, 68, 68)));
                border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(120, 239, 68, 68)));
                border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            }
            else
            {
                border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 41, 59))); // Slate 800
                border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85))); // Slate 700
                border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            }

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            if (isPrimary)
            {
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(3, 105, 161)), "bd")); // Sky 700
            }
            else if (isDanger)
            {
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(90, 239, 68, 68)), "bd"));
            }
            else
            {
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85)), "bd")); // Slate 700
            }
            template.Triggers.Add(hoverTrigger);
            btn.Template = template;

            // Direct Content StackPanel
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 12, 0)
            };

            var iconBrush = isPrimary ? Brushes.White : (isDanger ? new SolidColorBrush(Color.FromRgb(248, 113, 113)) : new SolidColorBrush(Color.FromRgb(203, 213, 225)));
            var icon = LucideIcon.Create(iconPath, 13, iconBrush, false, 1.8);
            icon.Margin = new Thickness(0, 0, 6, 0);
            sp.Children.Add(icon);

            TextBlock txt = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = isPrimary ? Brushes.White : (isDanger ? new SolidColorBrush(Color.FromRgb(248, 113, 113)) : new SolidColorBrush(Color.FromRgb(203, 213, 225))),
                FontFamily = new FontFamily("Segoe UI, Inter")
            };
            sp.Children.Add(txt);

            btn.Content = sp;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Button CreateHeaderButton(string iconPath, string tooltip, Action onClick)
        {
            Button btn = new Button
            {
                Width = 28,
                Height = 28,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0),
                ToolTip = tooltip,
                Focusable = false
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(160, 51, 65, 85)), "bd"));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            btn.Content = LucideIcon.Create(iconPath, 14, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 1.8);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private Button CreatePrimaryButton(string text, Action onClick)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 32,
                Padding = new Thickness(14, 0, 14, 0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0),
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(2, 132, 199))); // Sky 600

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(3, 105, 161)), "bd")); // Sky 700
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            btn.Click += (s, e) => onClick();
            return btn;
        }
    }
}
