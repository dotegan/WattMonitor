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
            this.Width = 460;
            this.Height = 580;
            this.MinWidth = 420;
            this.MinHeight = 480;
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
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Tab Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Footer

            // ================= 1. HEADER =================
            Border headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 12, 14, 12),
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
            titleSp.Children.Add(LucideIcon.Create(LucideIcon.Settings, 16, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false, 2.0));

            TextBlock titleTxt = new TextBlock
            {
                Text = " Pengaturan",
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                FontSize = 13.5,
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

            // ================= 2. TAB BAR (Pill Container) =================
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
            for (int i = 0; i < 5; i++)
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

            tabBtnPeripheral = CreateTabPillButton("Device", LucideIcon.HardDrive, 3, out tabTxtPeripheral);
            Grid.SetColumn(tabBtnPeripheral, 3);
            tabGrid.Children.Add(tabBtnPeripheral);

            tabBtnFilter = CreateTabPillButton("Filter", LucideIcon.Activity, 4, out tabTxtFilter);
            Grid.SetColumn(tabBtnFilter, 4);
            tabGrid.Children.Add(tabBtnFilter);

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

            scrollPeripheral = new ScrollViewer { Content = BuildPeripheralTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollPeripheral);

            scrollFilter = new ScrollViewer { Content = BuildFilterTab(), VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Visibility = Visibility.Collapsed };
            contentGrid.Children.Add(scrollFilter);

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
                Text = "",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), // Emerald 400
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(txtStatusMsg, 0);
            footerGrid.Children.Add(txtStatusMsg);

            // Action Buttons (Clear Labels + Icons)
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

            // Refresh rate slider
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
            sliderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64, GridUnitType.Pixel) });

            slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                SmallChange = tick,
                LargeChange = tick * 2,
                TickFrequency = tick,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 10, 0),
                Height = 22,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(slider, 0);
            sliderGrid.Children.Add(slider);

            Border valBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Height = 26,
                Padding = new Thickness(4, 0, 4, 0)
            };

            txtVal = new TextBlock
            {
                FontSize = 11,
                FontFamily = new FontFamily("Consolas, Segoe UI"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)), // Sky 400
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            valBadge.Child = txtVal;
            Grid.SetColumn(valBadge, 1);
            sliderGrid.Children.Add(valBadge);

            TextBlock capturedTxt = txtVal;
            slider.ValueChanged += (s, e) => capturedTxt.Text = formatVal(e.NewValue);

            rowSp.Children.Add(sliderGrid);
            return rowSp;
        }

        // ================= TAB 3: THRESHOLD SENSOR =================
        private UIElement BuildThresholdTab()
        {
            StackPanel sp = new StackPanel();

            Border cardCpu = CreateSectionCard("Batas Indikator CPU", "Nilai batas peralihan warna teks status CPU (Normal -> Oranye -> Merah).");
            StackPanel spCpu = new StackPanel();

            Grid gCpu = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            gCpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gCpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            gCpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel spCpuLeft = new StackPanel();
            tbCpuTempOrange = CreateInputFieldWithSuffix("Suhu Oranye", "°C", "55", spCpuLeft);
            tbCpuWattOrange = CreateInputFieldWithSuffix("Daya Oranye", "W", "45", spCpuLeft);
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

            Border cardGpu = CreateSectionCard("Batas Indikator GPU", "Nilai batas ambang peringatan suhu dan daya kartu grafis.");
            StackPanel spGpu = new StackPanel();

            Grid gGpu = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            gGpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gGpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12, GridUnitType.Pixel) });
            gGpu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel spGpuLeft = new StackPanel();
            tbGpuTempOrange = CreateInputFieldWithSuffix("Suhu Oranye", "°C", "60", spGpuLeft);
            tbGpuWattOrange = CreateInputFieldWithSuffix("Daya Oranye", "W", "95", spGpuLeft);
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

        // ================= TAB 4: PERIPHERAL =================
        private UIElement BuildPeripheralTab()
        {
            StackPanel sp = new StackPanel();

            Border cardList = CreateSectionCard("Perangkat Tambahan", "Konsumsi daya estimasi peripheral (monitor, fan, RAM, motherboard).");
            StackPanel cardSp = new StackPanel();

            // List container
            spPeripheralList = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            cardSp.Children.Add(spPeripheralList);

            // Add new form
            Border addBox = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 4, 0, 0)
            };

            Grid addGrid = new Grid();
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            addGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            tbNewPeriName = CreateDarkTextBox("Nama device (cth: Monitor)");
            Grid.SetColumn(tbNewPeriName, 0);
            addGrid.Children.Add(tbNewPeriName);

            tbNewPeriWatt = CreateDarkTextBox("Watt");
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
                Border emptyBox = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(80, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                emptyBox.Child = new TextBlock
                {
                    Text = "Belum ada peripheral terdaftar.",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spPeripheralList.Children.Add(emptyBox);
                return;
            }

            foreach (var kvp in new List<KeyValuePair<string, double>>(workingPeripherals))
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 7, 10, 7),
                    Margin = new Thickness(0, 0, 0, 5)
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBlock txtName = new TextBlock
                {
                    Text = kvp.Key,
                    FontSize = 11.5,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(txtName, 0);
                g.Children.Add(txtName);

                TextBlock txtWatt = new TextBlock
                {
                    Text = string.Format(CultureInfo.InvariantCulture, "{0:0.##} W", kvp.Value),
                    FontSize = 11.5,
                    FontFamily = new FontFamily("Consolas, Segoe UI"),
                    Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtWatt, 1);
                g.Children.Add(txtWatt);

                string keyCaptured = kvp.Key;
                Button btnDel = CreateHeaderButton(LucideIcon.Trash, "Hapus", () =>
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

            Border cardFilter = CreateSectionCard("Filter Teks Sensor", "Kata-kata berikut akan otomatis dipangkas dari nama hardware agar teks HUD ringkas.");
            StackPanel cardSp = new StackPanel();

            cardSp.Children.Add(CreateLabel("Daftar Teks yang Dipangkas (Pisahkan koma):"));
            tbExcludeTexts = CreateDarkTextBox("NVIDIA GeForce, Intel Core, (TM), ...");
            tbExcludeTexts.TextWrapping = TextWrapping.Wrap;
            tbExcludeTexts.AcceptsReturn = false;
            tbExcludeTexts.Height = 70;
            tbExcludeTexts.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            cardSp.Children.Add(tbExcludeTexts);

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
            if (MessageBox.Show("Kembalikan semua nilai ke konfigurasi standar default?", "Konfirmasi Reset", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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

                // Simpan ke config.json
                Config.Save();

                txtStatusMsg.Text = "Pengaturan berhasil disimpan.";

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
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)), // Slate 400
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
                Margin = new Thickness(0, 2, 0, 4)
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
                ToolTip = tooltip
            };
            return tb;
        }

        private Border CreateTabPillButton(string title, string iconPath, int tabIndex, out TextBlock txt)
        {
            Border b = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(2, 0, 2, 0),
                Cursor = Cursors.Hand
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(LucideIcon.Create(iconPath, 13, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 1.8));

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

            Border activeBtn = null;
            TextBlock activeTxt = null;
            ScrollViewer activeScroll = null;

            switch (tabIndex)
            {
                case 0: activeBtn = tabBtnGeneral; activeTxt = tabTxtGeneral; activeScroll = scrollGeneral; break;
                case 1: activeBtn = tabBtnDisplay; activeTxt = tabTxtDisplay; activeScroll = scrollDisplay; break;
                case 2: activeBtn = tabBtnThreshold; activeTxt = tabTxtThreshold; activeScroll = scrollThreshold; break;
                case 3: activeBtn = tabBtnPeripheral; activeTxt = tabTxtPeripheral; activeScroll = scrollPeripheral; break;
                case 4: activeBtn = tabBtnFilter; activeTxt = tabTxtFilter; activeScroll = scrollFilter; break;
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
                hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85)), "bd")); // Slate 700
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;

            // Content StackPanel
            StackPanel btnSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(12, 0, 12, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Brush iconBrush = isPrimary ? Brushes.White : new SolidColorBrush(Color.FromRgb(203, 213, 225));
            btnSp.Children.Add(LucideIcon.Create(iconPath, 13, iconBrush, false, 1.8));

            TextBlock txt = new TextBlock
            {
                Text = " " + text,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = isPrimary ? Brushes.White : new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                VerticalAlignment = VerticalAlignment.Center
            };
            btnSp.Children.Add(txt);

            btn.Content = btnSp;
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
