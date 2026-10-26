using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private Border tabBtnPeripheral;
        private Border tabBtnFilter;

        private TextBlock tabTxtGeneral;
        private TextBlock tabTxtDisplay;
        private TextBlock tabTxtThreshold;
        private TextBlock tabTxtPeripheral;
        private TextBlock tabTxtFilter;

        // Scroll Containers
        private ScrollViewer scrollGeneral;
        private ScrollViewer scrollDisplay;
        private ScrollViewer scrollThreshold;
        private ScrollViewer scrollPeripheral;
        private ScrollViewer scrollFilter;

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

        // Peripheral Tab Controls
        private StackPanel spPeripheralList;
        private TextBox tbNewPeriName;
        private TextBox tbNewPeriWatt;
        private Dictionary<string, double> workingPeripherals;

        // Filter Tab Inputs
        private TextBox tbExcludeTexts;

        // Footer
        private TextBlock txtStatusMsg;

        public SettingsWindow(Action onApplied = null)
        {
            this.Title = "Pengaturan - WattMonitor";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 410;
            this.Height = 560;
            this.MinWidth = 360;
            this.MinHeight = 460;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            this.onSettingsApplied = onApplied;
            this.workingPeripherals = new Dictionary<string, double>(Config.StaticPeripherals);

            ApplyModernScrollbarStyle();
            BuildUI();
            LoadCurrentValues();
            SelectTab(0);
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
                    <Track x:Name='PART_Track' IsDirectionReversed='true' EnableContextMenu='false'>
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

        private void BuildUI()
        {
            Border rootCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)), // #0B0F19
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // #1E293B
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                SnapsToDevicePixels = true
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tab Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ================= 1. HEADER =================
            Border headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 10, 16, 10),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };

            headerBorder.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left) this.DragMove();
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleSp.Children.Add(LucideIcon.Create(LucideIcon.Settings, 18, new SolidColorBrush(Color.FromRgb(168, 85, 247)), false));

            TextBlock titleTxt = new TextBlock
            {
                Text = " Pengaturan & Konfigurasi",
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
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

            // ================= 2. TAB BAR =================
            Border tabBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 6, 16, 6)
            };

            StackPanel tabSp = new StackPanel { Orientation = Orientation.Horizontal };
            tabBtnGeneral = CreateTabButton("Tarif", LucideIcon.Zap, 0, out tabTxtGeneral);
            tabSp.Children.Add(tabBtnGeneral);

            tabBtnDisplay = CreateTabButton("Layar", LucideIcon.Monitor, 1, out tabTxtDisplay);
            tabSp.Children.Add(tabBtnDisplay);

            tabBtnThreshold = CreateTabButton("Batas", LucideIcon.Thermometer, 2, out tabTxtThreshold);
            tabSp.Children.Add(tabBtnThreshold);

            tabBtnPeripheral = CreateTabButton("Device", LucideIcon.HardDrive, 3, out tabTxtPeripheral);
            tabSp.Children.Add(tabBtnPeripheral);

            tabBtnFilter = CreateTabButton("Filter", LucideIcon.Activity, 4, out tabTxtFilter);
            tabSp.Children.Add(tabBtnFilter);

            tabBar.Child = tabSp;
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

            scrollPeripheral = new ScrollViewer { Content = BuildPeripheralTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollPeripheral);

            scrollFilter = new ScrollViewer { Content = BuildFilterTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollFilter);

            Grid.SetRow(contentGrid, 2);
            mainGrid.Children.Add(contentGrid);

            // ================= 4. FOOTER =================
            Border footerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 8, 16, 8),
                CornerRadius = new CornerRadius(0, 0, 10, 10)
            };

            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Status message
            txtStatusMsg = new TextBlock
            {
                Text = "",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), // Emerald
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtStatusMsg, 0);
            footerGrid.Children.Add(txtStatusMsg);

            // Action Buttons (Pure Icon Buttons with Tooltips)
            StackPanel btnSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button btnReset = CreateIconActionButton(LucideIcon.RotateCcw, "Reset ke Pengaturan Default", ResetToDefaults, false);
            btnSp.Children.Add(btnReset);

            Button btnCancel = CreateIconActionButton(LucideIcon.X, "Batal / Tutup", () => this.Close(), false);
            btnSp.Children.Add(btnCancel);

            Button btnSave = CreateIconActionButton(LucideIcon.Save, "Simpan Pengaturan & Terapkan", SaveAndApplySettings, true);
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

            Border cardTariff = CreateSectionCard("Kalkulasi Tarif & Biaya Listrik", "Pengaturan tarif listrik PLN untuk kalkulasi estimasi pengeluaran.");
            StackPanel cardSp = new StackPanel();

            tbTariff = CreateInputField("Tarif Listrik (Rp / kWh):", "Contoh: 1444.70 untuk R-1/TR daya 900-1300 VA.", cardSp);
            tbHoursPerDay = CreateInputField("Estimasi Jam Aktif per Hari (Jam):", "Digunakan untuk perkiraan tagihan bulanan (default: 12 jam).", cardSp);
            tbAutosaveMinutes = CreateInputField("Interval Simpan Data Otomatis (Menit):", "Contoh: 0.08 (~5 detik) untuk keamanan proteksi mati lampu.", cardSp);
            tbWattOffset = CreateInputField("Tambahan Daya Tetap (Watt Offset):", "Kompensasi daya standby yang tidak terdeteksi sensor (default: 5).", cardSp);

            cardTariff.Child = cardSp;
            sp.Children.Add(cardTariff);

            return sp;
        }

        // ================= TAB 2: TAMPILAN & OVERLAY =================
        private UIElement BuildDisplayTab()
        {
            StackPanel sp = new StackPanel();

            Border cardDisplay = CreateSectionCard("Window & Mode Minimalis", "Sesuaikan tingkat transparansi dan ukuran teks widget di layar.");
            StackPanel cardSp = new StackPanel();

            // Refresh rate slider
            cardSp.Children.Add(CreateLabel("Refresh Rate Sensor (Kecepatan Update):"));
            sliderRefreshRate = new Slider { Minimum = 100, Maximum = 2000, SmallChange = 50, LargeChange = 100, TickFrequency = 100, IsSnapToTickEnabled = true, Margin = new Thickness(0, 4, 0, 2) };
            txtRefreshRateVal = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 0, 0, 12) };
            sliderRefreshRate.ValueChanged += (s, e) => txtRefreshRateVal.Text = string.Format("{0} ms ({1:0.1} detik)", (int)sliderRefreshRate.Value, sliderRefreshRate.Value / 1000.0);
            cardSp.Children.Add(sliderRefreshRate);
            cardSp.Children.Add(txtRefreshRateVal);

            // Full Opacity
            cardSp.Children.Add(CreateLabel("Transparansi Window Utama:"));
            sliderOpacity = new Slider { Minimum = 0.2, Maximum = 1.0, SmallChange = 0.05, LargeChange = 0.1, Margin = new Thickness(0, 4, 0, 2) };
            txtOpacityVal = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 0, 0, 12) };
            sliderOpacity.ValueChanged += (s, e) => txtOpacityVal.Text = string.Format("{0}%", (int)(sliderOpacity.Value * 100));
            cardSp.Children.Add(sliderOpacity);
            cardSp.Children.Add(txtOpacityVal);

            // Minimalist Font Size
            cardSp.Children.Add(CreateLabel("Ukuran Font Mode Minimalis (pt):"));
            sliderFontSize = new Slider { Minimum = 8, Maximum = 18, SmallChange = 1, LargeChange = 2, TickFrequency = 1, IsSnapToTickEnabled = true, Margin = new Thickness(0, 4, 0, 2) };
            txtFontSizeVal = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 0, 0, 12) };
            sliderFontSize.ValueChanged += (s, e) => txtFontSizeVal.Text = string.Format("{0} pt", (int)sliderFontSize.Value);
            cardSp.Children.Add(sliderFontSize);
            cardSp.Children.Add(txtFontSizeVal);

            // Minimalist Opacity
            cardSp.Children.Add(CreateLabel("Transparansi Mode Minimalis:"));
            sliderMinOpacity = new Slider { Minimum = 0.2, Maximum = 1.0, SmallChange = 0.05, LargeChange = 0.1, Margin = new Thickness(0, 4, 0, 2) };
            txtMinOpacityVal = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), Margin = new Thickness(0, 0, 0, 6) };
            sliderMinOpacity.ValueChanged += (s, e) => txtMinOpacityVal.Text = string.Format("{0}%", (int)(sliderMinOpacity.Value * 100));
            cardSp.Children.Add(sliderMinOpacity);
            cardSp.Children.Add(txtMinOpacityVal);

            cardDisplay.Child = cardSp;
            sp.Children.Add(cardDisplay);

            return sp;
        }

        // ================= TAB 3: THRESHOLD SENSOR =================
        private UIElement BuildThresholdTab()
        {
            StackPanel sp = new StackPanel();

            Border cardCpu = CreateSectionCard("Batas Suhu & Daya CPU", "Teks warna indikator akan berubah dari Normal ke Oranye, lalu ke Merah.");
            StackPanel spCpu = new StackPanel();
            tbCpuTempOrange = CreateInputField("Batas Suhu CPU Oranye (°C):", "Default: 55 °C", spCpu);
            tbCpuTempRed = CreateInputField("Batas Suhu CPU Merah (°C):", "Default: 68 °C", spCpu);
            tbCpuWattOrange = CreateInputField("Batas Daya CPU Oranye (Watt):", "Default: 45 W", spCpu);
            tbCpuWattRed = CreateInputField("Batas Daya CPU Merah (Watt):", "Default: 60 W", spCpu);
            cardCpu.Child = spCpu;
            sp.Children.Add(cardCpu);

            Border cardGpu = CreateSectionCard("Batas Suhu & Daya GPU", "Ambang batas peringatan temperatur dan watt kartu grafis.");
            StackPanel spGpu = new StackPanel();
            tbGpuTempOrange = CreateInputField("Batas Suhu GPU Oranye (°C):", "Default: 60 °C", spGpu);
            tbGpuTempRed = CreateInputField("Batas Suhu GPU Merah (°C):", "Default: 72 °C", spGpu);
            tbGpuWattOrange = CreateInputField("Batas Daya GPU Oranye (Watt):", "Default: 95 W", spGpu);
            tbGpuWattRed = CreateInputField("Batas Daya GPU Merah (Watt):", "Default: 130 W", spGpu);
            cardGpu.Child = spGpu;
            sp.Children.Add(cardGpu);

            return sp;
        }

        // ================= TAB 4: PERIPHERAL =================
        private UIElement BuildPeripheralTab()
        {
            StackPanel sp = new StackPanel();

            Border cardList = CreateSectionCard("Perangkat Tambahan (Peripheral)", "Tambahkan konsumsi daya estimasi perangkat yang tidak memiliki sensor built-in (monitor, fan, RAM, motherboard).");
            StackPanel cardSp = new StackPanel();

            // List container
            spPeripheralList = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            cardSp.Children.Add(spPeripheralList);

            // Add new form
            Border addBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 4, 0, 0)
            };

            Grid addGrid = new Grid();
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            tbNewPeriName = CreateDarkTextBox("Nama Perangkat (cth: Monitor LG 27\")");
            Grid.SetColumn(tbNewPeriName, 0);
            addGrid.Children.Add(tbNewPeriName);

            tbNewPeriWatt = CreateDarkTextBox("Daya (Watt)");
            Grid.SetColumn(tbNewPeriWatt, 2);
            addGrid.Children.Add(tbNewPeriWatt);

            Button btnAdd = CreatePrimaryButton("+ Tambah", () =>
            {
                string name = tbNewPeriName.Text.Trim();
                double w;
                if (!string.IsNullOrEmpty(name) && double.TryParse(tbNewPeriWatt.Text.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out w))
                {
                    workingPeripherals[name] = w;
                    tbNewPeriName.Text = "";
                    tbNewPeriWatt.Text = "";
                    RenderPeripheralList();
                }
                else
                {
                    MessageBox.Show("Mohon masukkan nama perangkat dan angka watt yang valid.", "Input Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            });
            btnAdd.Height = 32;
            Grid.SetColumn(btnAdd, 4);
            addGrid.Children.Add(btnAdd);

            addBox.Child = addGrid;
            cardSp.Children.Add(addBox);

            cardList.Child = cardSp;
            sp.Children.Add(cardList);

            return sp;
        }

        private void RenderPeripheralList()
        {
            spPeripheralList.Children.Clear();
            if (workingPeripherals.Count == 0)
            {
                TextBlock emptyTxt = new TextBlock
                {
                    Text = "Belum ada peripheral yang didaftarkan.",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 11,
                    Margin = new Thickness(0, 4, 0, 8)
                };
                spPeripheralList.Children.Add(emptyTxt);
                return;
            }

            foreach (var kvp in new List<KeyValuePair<string, double>>(workingPeripherals))
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(20, 27, 45)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock txtName = new TextBlock
                {
                    Text = kvp.Key,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtName, 0);
                g.Children.Add(txtName);

                TextBlock txtWatt = new TextBlock
                {
                    Text = string.Format(CultureInfo.InvariantCulture, "{0:0.##} W", kvp.Value),
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtWatt, 1);
                g.Children.Add(txtWatt);

                string keyCaptured = kvp.Key;
                Button btnDel = CreateHeaderButton(LucideIcon.Trash, "Hapus Peripheral", () =>
                {
                    workingPeripherals.Remove(keyCaptured);
                    RenderPeripheralList();
                });
                btnDel.Width = 24;
                btnDel.Height = 24;
                Grid.SetColumn(btnDel, 3);
                g.Children.Add(btnDel);

                row.Child = g;
                spPeripheralList.Children.Add(row);
            }
        }

        // ================= TAB 5: FILTER NAMA =================
        private UIElement BuildFilterTab()
        {
            StackPanel sp = new StackPanel();

            Border cardFilter = CreateSectionCard("Filter Teks Sensor", "Kata-kata berikut akan otomatis dipotong dari nama perangkat agar ringkas di HUD.");
            StackPanel cardSp = new StackPanel();

            tbExcludeTexts = CreateInputField("Daftar Teks yang Dihapus (Pisahkan dengan koma):", "Contoh: NVIDIA GeForce, Intel Core, (TM), Processor, 8-Core", cardSp);

            cardFilter.Child = cardSp;
            sp.Children.Add(cardFilter);

            return sp;
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

            tbExcludeTexts.Text = string.Join(", ", Config.ExcludeTexts);

            workingPeripherals = new Dictionary<string, double>(Config.StaticPeripherals);
            RenderPeripheralList();
        }

        private void ResetToDefaults()
        {
            if (MessageBox.Show("Kembalikan semua nilai ke pengaturan standar (default)?", "Konfirmasi Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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

            tbExcludeTexts.Text = "NVIDIA GeForce, (TM), Processor, Intel Core, 8-Core, 16-Core, 12-Core, 6-Core, 4-Core";
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

                Config.ExcludeTexts.Clear();
                foreach (string item in tbExcludeTexts.Text.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(item)) Config.ExcludeTexts.Add(item.Trim());
                }

                Config.StaticPeripherals.Clear();
                foreach (var kvp in workingPeripherals)
                {
                    Config.StaticPeripherals[kvp.Key] = kvp.Value;
                }

                // Simpan ke config.txt
                Config.Save();

                txtStatusMsg.Text = "Pengaturan berhasil disimpan dan diterapkan.";

                // Trigger reload pada window utama
                if (onSettingsApplied != null) onSettingsApplied();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Terjadi kesalahan saat menyimpan pengaturan: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= HELPERS & STYLES =================
        private Border CreateSectionCard(string title, string subtitle)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // #0F172A
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // #1E293B
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };

            StackPanel sp = new StackPanel();
            TextBlock tTxt = new TextBlock
            {
                Text = title,
                FontSize = 13,
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
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Margin = new Thickness(0, 2, 0, 12)
                };
                sp.Children.Add(sTxt);
            }

            b.Child = sp;
            return b;
        }

        private TextBox CreateInputField(string labelText, string hintText, StackPanel parent)
        {
            parent.Children.Add(CreateLabel(labelText));
            TextBox tb = CreateDarkTextBox(hintText);
            parent.Children.Add(tb);
            return tb;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225)), // Slate 300
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        private TextBox CreateDarkTextBox(string tooltip = "")
        {
            TextBox tb = new TextBox
            {
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate 700
                BorderThickness = new Thickness(1),
                Foreground = Brushes.White,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 10),
                ToolTip = tooltip
            };
            return tb;
        }

        private Border CreateTabButton(string title, string iconPath, int tabIndex, out TextBlock txt)
        {
            Border b = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 4, 7, 4),
                Margin = new Thickness(0, 0, 3, 0),
                Cursor = Cursors.Hand
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(LucideIcon.Create(iconPath, 13, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false));

            txt = new TextBlock
            {
                Text = " " + title,
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(txt);
            b.Child = sp;

            b.MouseDown += (s, e) => SelectTab(tabIndex);
            return b;
        }

        private void SelectTab(int tabIndex)
        {
            currentTab = tabIndex;

            tabBtnGeneral.Background = Brushes.Transparent;
            tabTxtGeneral.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnDisplay.Background = Brushes.Transparent;
            tabTxtDisplay.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnThreshold.Background = Brushes.Transparent;
            tabTxtThreshold.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnPeripheral.Background = Brushes.Transparent;
            tabTxtPeripheral.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnFilter.Background = Brushes.Transparent;
            tabTxtFilter.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            scrollGeneral.Visibility = Visibility.Collapsed;
            scrollDisplay.Visibility = Visibility.Collapsed;
            scrollThreshold.Visibility = Visibility.Collapsed;
            scrollPeripheral.Visibility = Visibility.Collapsed;
            scrollFilter.Visibility = Visibility.Collapsed;

            if (tabIndex == 0)
            {
                tabBtnGeneral.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtGeneral.Foreground = Brushes.White;
                scrollGeneral.Visibility = Visibility.Visible;
            }
            else if (tabIndex == 1)
            {
                tabBtnDisplay.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtDisplay.Foreground = Brushes.White;
                scrollDisplay.Visibility = Visibility.Visible;
            }
            else if (tabIndex == 2)
            {
                tabBtnThreshold.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtThreshold.Foreground = Brushes.White;
                scrollThreshold.Visibility = Visibility.Visible;
            }
            else if (tabIndex == 3)
            {
                tabBtnPeripheral.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtPeripheral.Foreground = Brushes.White;
                scrollPeripheral.Visibility = Visibility.Visible;
            }
            else if (tabIndex == 4)
            {
                tabBtnFilter.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtFilter.Foreground = Brushes.White;
                scrollFilter.Visibility = Visibility.Visible;
            }
        }

        private Button CreateIconActionButton(string iconPath, string tooltip, Action onClick, bool isPrimary)
        {
            Button btn = new Button
            {
                Width = 34,
                Height = 30,
                Cursor = Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0),
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
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(3, 105, 161)), "bd")); // Sky 700
            else
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85)), "bd"));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            Brush iconBrush = isPrimary ? Brushes.White : new SolidColorBrush(Color.FromRgb(203, 213, 225));
            btn.Content = LucideIcon.Create(iconPath, 15, iconBrush, false, 1.8);
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
                Height = 30,
                Padding = new Thickness(14, 0, 14, 0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(6, 0, 0, 0),
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "bd";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(2, 132, 199))); // #0284C7 Sky 600

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

        private Button CreateOutlineButton(string text, Action onClick)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 30,
                Padding = new Thickness(12, 0, 12, 0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(6, 0, 0, 0),
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(203, 213, 225))
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.Name = "bd";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 41, 59))); // Slate 800
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85)), "bd"));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            btn.Click += (s, e) => onClick();
            return btn;
        }
    }
}
