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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WattMonitor
{
    public class ReportViewerWindow : Window
    {
        private DateTime selectedMonth;
        private int currentTab = 0; // 0 = Pemakaian Harian, 1 = Konsumsi Perangkat, 2 = Informasi
        private Func<LiveSessionSnapshot> liveDataProvider;
        private DispatcherTimer liveTimer;

        // Tab Buttons & Text
        private Border tabBtnUsage;
        private Border tabBtnHardware;
        private Border tabBtnInfo;
        private TextBlock tabTxtUsage;
        private TextBlock tabTxtHardware;
        private TextBlock tabTxtInfo;

        // Content Containers
        private ScrollViewer scrollUsage;
        private ScrollViewer scrollHardware;
        private ScrollViewer scrollInfo;
        private StackPanel spUsageContent;
        private StackPanel spHardwareContent;
        private StackPanel spInfoContent;

        // Custom Dark Month Selector
        private Border btnMonthSelector;
        private TextBlock txtMonthSelectorLabel;
        private Popup popupMonths;
        private StackPanel spMonthItems;

        public ReportViewerWindow(int initialTab = 0, Func<LiveSessionSnapshot> liveSnapshotProvider = null)
        {
            this.Title = "Laporan & Statistik Pemakaian Daya - WattMonitor";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 780;
            this.Height = 580;
            this.MinWidth = 620;
            this.MinHeight = 460;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            selectedMonth = DateTime.Now;
            currentTab = initialTab;
            liveDataProvider = liveSnapshotProvider;

            BuildUI();
            LoadAvailableMonths();
            RefreshData();
            SelectTab(currentTab);

            // Live Update Timer (ticks every 1 second)
            liveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            liveTimer.Tick += (s, e) =>
            {
                if (IsCurrentMonthSelected())
                {
                    RefreshData();
                }
            };
            liveTimer.Start();

            this.Closed += (s, e) =>
            {
                if (liveTimer != null) liveTimer.Stop();
            };
        }

        private bool IsCurrentMonthSelected()
        {
            return selectedMonth.Year == DateTime.Now.Year && selectedMonth.Month == DateTime.Now.Month;
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
            catch (Exception ex)
            {
                AppLogger.Error("Gagal memuat custom modern scrollbar style", ex);
            }
        }

        private void BuildUI()
        {
            ApplyModernScrollbarStyle();

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

            // ================= HEADER =================
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
                if (e.ChangedButton == MouseButton.Left)
                {
                    if (e.OriginalSource == headerBorder || (e.OriginalSource is Visual && ((Visual)e.OriginalSource).IsDescendantOf(headerBorder) && !IsInsideElement((DependencyObject)e.OriginalSource, btnMonthSelector)))
                    {
                        this.DragMove();
                    }
                }
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Title & Icon & Month Selector
            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            titleSp.Children.Add(LucideIcon.Create(LucideIcon.BarChart, 18, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false));
            
            TextBlock titleTxt = new TextBlock
            {
                Text = " Laporan Pemakaian & Biaya",
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(titleTxt);

            // Custom Dark Month Selector Button
            BuildMonthSelector();
            titleSp.Children.Add(btnMonthSelector);

            Grid.SetColumn(titleSp, 0);
            headerGrid.Children.Add(titleSp);

            // Header Action Buttons (Open MD, Minimize, Close)
            StackPanel actSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            Button btnOpenMd = CreateHeaderButton(LucideIcon.FileText, "Buka Berkas .MD Asli", () => OpenCurrentMdFile());
            actSp.Children.Add(btnOpenMd);

            Button btnMin = CreateHeaderButton(LucideIcon.Minimize, "Minimize", () => this.WindowState = WindowState.Minimized);
            actSp.Children.Add(btnMin);

            Button btnClose = CreateHeaderButton(LucideIcon.X, "Tutup", () => this.Close());
            actSp.Children.Add(btnClose);

            Grid.SetColumn(actSp, 1);
            headerGrid.Children.Add(actSp);

            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // ================= TAB BAR =================
            Border tabBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 6, 16, 6)
            };

            StackPanel tabSp = new StackPanel { Orientation = Orientation.Horizontal };

            tabBtnUsage = CreateTabButton("Pemakaian Harian", LucideIcon.Activity, 0, out tabTxtUsage);
            tabSp.Children.Add(tabBtnUsage);

            tabBtnHardware = CreateTabButton("Konsumsi Perangkat", LucideIcon.Cpu, 1, out tabTxtHardware);
            tabSp.Children.Add(tabBtnHardware);

            tabBtnInfo = CreateTabButton("Informasi", LucideIcon.Info, 2, out tabTxtInfo);
            tabSp.Children.Add(tabBtnInfo);

            tabBar.Child = tabSp;
            Grid.SetRow(tabBar, 1);
            mainGrid.Children.Add(tabBar);

            // ================= CONTENT AREA =================
            Grid contentGrid = new Grid { Margin = new Thickness(16, 12, 16, 12) };

            // Tab 1: Pemakaian Content
            spUsageContent = new StackPanel();
            scrollUsage = new ScrollViewer
            {
                Content = spUsageContent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            contentGrid.Children.Add(scrollUsage);

            // Tab 2: Hardware Content
            spHardwareContent = new StackPanel();
            scrollHardware = new ScrollViewer
            {
                Content = spHardwareContent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Visibility = Visibility.Collapsed
            };
            contentGrid.Children.Add(scrollHardware);

            // Tab 3: Information Content
            spInfoContent = new StackPanel();
            scrollInfo = new ScrollViewer
            {
                Content = spInfoContent,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Visibility = Visibility.Collapsed
            };
            contentGrid.Children.Add(scrollInfo);

            Grid.SetRow(contentGrid, 2);
            mainGrid.Children.Add(contentGrid);

            // ================= FOOTER =================
            Border footerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 6, 16, 6),
                CornerRadius = new CornerRadius(0, 0, 10, 10)
            };

            Grid footerGrid = new Grid();
            TextBlock txtFooter = new TextBlock
            {
                Text = string.Format("Tarif Listrik: Rp {0:N2} / kWh · Data tersimpan otomatis di system/laporan/", Config.Tariff),
                FontSize = 10.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                VerticalAlignment = VerticalAlignment.Center
            };
            footerGrid.Children.Add(txtFooter);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 3);
            mainGrid.Children.Add(footerBorder);

            rootCard.Child = mainGrid;
            this.Content = rootCard;
        }

        private void BuildMonthSelector()
        {
            btnMonthSelector = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // #1E293B
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // #334155
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(14, 0, 0, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel btnSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            txtMonthSelectorLabel = new TextBlock
            {
                Text = selectedMonth.ToString("MMMM yyyy", new CultureInfo("id-ID")),
                FontSize = 11.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnSp.Children.Add(txtMonthSelectorLabel);

            TextBlock arrowTxt = new TextBlock
            {
                Text = " ▾",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, -1, 0, 0)
            };
            btnSp.Children.Add(arrowTxt);
            btnMonthSelector.Child = btnSp;

            // Month Dropdown Popup
            popupMonths = new Popup
            {
                PlacementTarget = btnMonthSelector,
                Placement = PlacementMode.Bottom,
                StaysOpen = true,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.Fade
            };

            Border popupBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // #0F172A
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // #334155
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(4),
                MinWidth = 160,
                Margin = new Thickness(0, 4, 0, 0)
            };

            spMonthItems = new StackPanel();
            popupBorder.Child = spMonthItems;
            popupMonths.Child = popupBorder;

            btnMonthSelector.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                popupMonths.IsOpen = !popupMonths.IsOpen;
            };

            // Close popup when clicking outside
            this.PreviewMouseDown += (s, e) =>
            {
                if (popupMonths.IsOpen)
                {
                    DependencyObject src = e.OriginalSource as DependencyObject;
                    if (!IsInsideElement(src, btnMonthSelector) && !IsInsideElement(src, popupBorder))
                    {
                        popupMonths.IsOpen = false;
                    }
                }
            };

            this.Deactivated += (s, e) =>
            {
                if (popupMonths.IsOpen) popupMonths.IsOpen = false;
            };
        }

        private void LoadAvailableMonths()
        {
            spMonthItems.Children.Clear();
            string laporanDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "laporan");
            var monthList = new List<string>();

            if (Directory.Exists(laporanDir))
            {
                var dirs = Directory.GetDirectories(laporanDir);
                foreach (var d in dirs)
                {
                    string folderName = System.IO.Path.GetFileName(d);
                    if (folderName.Length == 7 && folderName.Contains("-"))
                    {
                        monthList.Add(folderName);
                    }
                }
            }

            string currentMonthStr = DateTime.Now.ToString("yyyy-MM");
            if (!monthList.Contains(currentMonthStr)) monthList.Add(currentMonthStr);
            monthList.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal)); // Descending

            foreach (var m in monthList)
            {
                DateTime dt;
                string display = m;
                if (DateTime.TryParseExact(m, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                {
                    display = dt.ToString("MMMM yyyy", new CultureInfo("id-ID"));
                }

                Border item = new Border
                {
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 6, 10, 6),
                    Cursor = Cursors.Hand
                };

                bool isSelected = (m == selectedMonth.ToString("yyyy-MM"));

                StackPanel itemSp = new StackPanel { Orientation = Orientation.Horizontal };
                if (isSelected)
                {
                    itemSp.Children.Add(new TextBlock
                    {
                        Text = "✓ ",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                TextBlock itemTxt = new TextBlock
                {
                    Text = display,
                    FontSize = 11.5,
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Foreground = isSelected ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) : Brushes.White,
                    FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                    VerticalAlignment = VerticalAlignment.Center
                };
                itemSp.Children.Add(itemTxt);
                item.Child = itemSp;

                item.MouseEnter += (s, e) => { item.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)); };
                item.MouseLeave += (s, e) => { item.Background = Brushes.Transparent; };

                string captureMonth = m;
                DateTime captureDt = dt;
                item.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    selectedMonth = captureDt;
                    txtMonthSelectorLabel.Text = captureDt.ToString("MMMM yyyy", new CultureInfo("id-ID"));
                    popupMonths.IsOpen = false;
                    LoadAvailableMonths(); // Update checkmarks
                    RefreshData();
                };

                spMonthItems.Children.Add(item);
            }
        }

        private Border CreateTabButton(string title, string iconPath, int tabIndex, out TextBlock txt)
        {
            Border b = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };

            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(LucideIcon.Create(iconPath, 14, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false));

            txt = new TextBlock
            {
                Text = " " + title,
                FontSize = 12,
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

        public void SelectTab(int tabIndex)
        {
            currentTab = tabIndex;

            // Reset all
            tabBtnUsage.Background = Brushes.Transparent;
            tabTxtUsage.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnHardware.Background = Brushes.Transparent;
            tabTxtHardware.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
            tabBtnInfo.Background = Brushes.Transparent;
            tabTxtInfo.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));

            scrollUsage.Visibility = Visibility.Collapsed;
            scrollHardware.Visibility = Visibility.Collapsed;
            scrollInfo.Visibility = Visibility.Collapsed;

            if (currentTab == 0)
            {
                tabBtnUsage.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtUsage.Foreground = Brushes.White;
                scrollUsage.Visibility = Visibility.Visible;
            }
            else if (currentTab == 1)
            {
                tabBtnHardware.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtHardware.Foreground = Brushes.White;
                scrollHardware.Visibility = Visibility.Visible;
            }
            else
            {
                tabBtnInfo.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
                tabTxtInfo.Foreground = Brushes.White;
                scrollInfo.Visibility = Visibility.Visible;
            }
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

            Trigger pressTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(220, 71, 85, 105)), "bd"));
            template.Triggers.Add(pressTrigger);

            btn.Template = template;
            btn.Content = LucideIcon.Create(iconPath, 14, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 1.8);
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void RefreshData()
        {
            RenderUsageTab();
            RenderHardwareTab();
            RenderInfoTab();
        }

        private void RenderUsageTab()
        {
            spUsageContent.Children.Clear();
            var records = HistoryStore.Load(selectedMonth);

            // Merge Live uncommitted session data if current month
            LiveSessionSnapshot live = null;
            if (IsCurrentMonthSelected() && liveDataProvider != null)
            {
                live = liveDataProvider();
                if (live != null && (live.UncommittedKwh > 0 || live.UncommittedHours > 0))
                {
                    string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                    var todayRec = records.Find(x => x.Date == todayStr);
                    if (todayRec == null)
                    {
                        todayRec = new SessionRecord
                        {
                            Date = todayStr,
                            Minutes = live.UncommittedHours * 60.0,
                            Kwh = live.UncommittedKwh,
                            Cost = live.UncommittedCost
                        };
                        records.Add(todayRec);
                    }
                    else
                    {
                        todayRec.Minutes += live.UncommittedHours * 60.0;
                        todayRec.Kwh += live.UncommittedKwh;
                        todayRec.Cost += live.UncommittedCost;
                    }
                }
            }

            double totalMinutes = 0, totalKwh = 0, totalCost = 0;
            foreach (var r in records)
            {
                totalMinutes += r.Minutes;
                totalKwh += r.Kwh;
                totalCost += r.Cost;
            }
            double avgCost = records.Count > 0 ? totalCost / records.Count : 0;

            // 1. SUMMARY STAT CARDS (4 Kolom)
            Grid sumGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            for (int i = 0; i < 4; i++)
            {
                sumGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (i < 3) sumGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8, GridUnitType.Pixel) });
            }

            TimeSpan ts = TimeSpan.FromMinutes(totalMinutes);
            string durStr = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);

            Border cardDur = CreateStatCard("DURASI AKTIF", durStr, "Total jam pemakaian", Color.FromRgb(56, 189, 248));
            Grid.SetColumn(cardDur, 0); sumGrid.Children.Add(cardDur);

            Border cardKwh = CreateStatCard("TOTAL ENERGI", string.Format(CultureInfo.InvariantCulture, "{0:F2} kWh", totalKwh), "Akumulasi pemakaian", Color.FromRgb(245, 158, 11));
            Grid.SetColumn(cardKwh, 2); sumGrid.Children.Add(cardKwh);

            Border cardCost = CreateStatCard("TOTAL BIAYA", string.Format("Rp {0:N0}", totalCost), string.Format("Tarif Rp {0:N0}/kWh", Config.Tariff), Color.FromRgb(74, 222, 128));
            Grid.SetColumn(cardCost, 4); sumGrid.Children.Add(cardCost);

            Border cardAvg = CreateStatCard("RATA-RATA HARIAN", string.Format("Rp {0:N0}", avgCost), string.Format("{0} hari tercatat", records.Count), Color.FromRgb(168, 85, 247));
            Grid.SetColumn(cardAvg, 6); sumGrid.Children.Add(cardAvg);

            spUsageContent.Children.Add(sumGrid);

            // 2. DAILY BREAKDOWN TABLE
            TextBlock tableTitle = new TextBlock
            {
                Text = "CATATAN PEMAKAIAN HARIAN",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 0, 0, 8)
            };
            spUsageContent.Children.Add(tableTitle);

            Border tableCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8)
            };

            StackPanel tableSp = new StackPanel();

            // Table Header Row
            Grid thGrid = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            thGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

            thGrid.Children.Add(CreateThText("TANGGAL", 0));
            thGrid.Children.Add(CreateThText("DURASI", 1));
            thGrid.Children.Add(CreateThText("KONSUMSI", 2));
            thGrid.Children.Add(CreateThText("ESTIMASI BIAYA", 3, HorizontalAlignment.Right));
            tableSp.Children.Add(thGrid);

            tableSp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(70, 51, 65, 85)), Margin = new Thickness(0, 0, 0, 4) });

            var sorted = new List<SessionRecord>(records);
            sorted.Sort((a, b) => string.Compare(b.Date, a.Date, StringComparison.Ordinal)); // Newest first

            if (sorted.Count == 0)
            {
                TextBlock emptyTxt = new TextBlock
                {
                    Text = "Belum ada riwayat sesi tercatat untuk bulan ini.",
                    FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 16, 0, 16),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                tableSp.Children.Add(emptyTxt);
            }
            else
            {
                string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                for (int i = 0; i < sorted.Count; i++)
                {
                    var r = sorted[i];
                    TimeSpan rTs = TimeSpan.FromMinutes(r.Minutes);
                    string rDur = string.Format("{0,2}:{1:D2}:{2:D2}", (int)rTs.TotalHours, rTs.Minutes, rTs.Seconds);
                    string rKwh = string.Format(CultureInfo.InvariantCulture, "{0:F4} kWh", r.Kwh);
                    string rCost = string.Format("Rp {0:N2}", r.Cost);

                    Grid tr = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                    tr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                    tr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
                    tr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
                    tr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

                    bool isToday = (r.Date == todayStr);

                    // Tanggal
                    StackPanel dtSp = new StackPanel { Orientation = Orientation.Horizontal };
                    TextBlock tDate = new TextBlock { Text = r.Date, FontSize = 12, FontFamily = new FontFamily("Consolas"), Foreground = isToday ? new SolidColorBrush(Color.FromRgb(74, 222, 128)) : Brushes.White, FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal };
                    dtSp.Children.Add(tDate);
                    if (isToday)
                    {
                        dtSp.Children.Add(new TextBlock { Text = " (Hari Ini)", FontSize = 10, FontFamily = new FontFamily("Segoe UI, Inter"), Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)), Margin = new Thickness(4, 1, 0, 0) });
                    }
                    Grid.SetColumn(dtSp, 0); tr.Children.Add(dtSp);

                    // Durasi
                    TextBlock tDur = new TextBlock { Text = rDur, FontSize = 12, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)) };
                    Grid.SetColumn(tDur, 1); tr.Children.Add(tDur);

                    // Kwh
                    TextBlock tKwh = new TextBlock { Text = rKwh, FontSize = 12, FontFamily = new FontFamily("Consolas"), Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)) };
                    Grid.SetColumn(tKwh, 2); tr.Children.Add(tKwh);

                    // Biaya
                    TextBlock tCost = new TextBlock { Text = rCost, FontSize = 12, FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128)), HorizontalAlignment = HorizontalAlignment.Right };
                    Grid.SetColumn(tCost, 3); tr.Children.Add(tCost);

                    tableSp.Children.Add(tr);

                    if (i < sorted.Count - 1)
                    {
                        tableSp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(40, 51, 65, 85)), Margin = new Thickness(0, 3, 0, 3) });
                    }
                }
            }

            tableCard.Child = tableSp;
            spUsageContent.Children.Add(tableCard);
        }

        private void RenderHardwareTab()
        {
            spHardwareContent.Children.Clear();
            var hwRecords = HistoryStore.LoadHardware(selectedMonth);
            var sessionRecords = HistoryStore.Load(selectedMonth);

            // Merge Live uncommitted hardware session data if current month
            if (IsCurrentMonthSelected() && liveDataProvider != null)
            {
                LiveSessionSnapshot live = liveDataProvider();
                if (live != null && live.UncommittedHardwareKwh != null)
                {
                    string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                    foreach (var kvp in live.UncommittedHardwareKwh)
                    {
                        var existing = hwRecords.Find(x => x.Date == todayStr && x.Name == kvp.Key);
                        if (existing != null)
                        {
                            existing.Kwh += kvp.Value;
                            existing.Cost += kvp.Value * Config.Tariff;
                        }
                        else
                        {
                            hwRecords.Add(new HardwareRecord
                            {
                                Date = todayStr,
                                Name = kvp.Key,
                                Kwh = kvp.Value,
                                Cost = kvp.Value * Config.Tariff
                            });
                        }
                    }
                }
            }

            // Group by hardware name
            var hwTotals = new Dictionary<string, HardwareRecord>();
            double totalHwKwh = 0;
            foreach (var r in hwRecords)
            {
                if (!hwTotals.ContainsKey(r.Name)) hwTotals[r.Name] = new HardwareRecord { Name = r.Name, Kwh = 0, Cost = 0 };
                hwTotals[r.Name].Kwh += r.Kwh;
                hwTotals[r.Name].Cost += r.Cost;
                totalHwKwh += r.Kwh;
            }

            var sortedHw = new List<HardwareRecord>(hwTotals.Values);
            sortedHw.Sort((a, b) => b.Kwh.CompareTo(a.Kwh)); // Highest consumption first

            int daysCount = sessionRecords.Count > 0 ? sessionRecords.Count : 1;

            TextBlock secTitle = new TextBlock
            {
                Text = "RINGKASAN KONSUMSI PER KOMPONEN / PERANGKAT",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 0, 0, 8)
            };
            spHardwareContent.Children.Add(secTitle);

            if (sortedHw.Count == 0)
            {
                Border emptyCard = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16)
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "Belum ada riwayat perangkat tercatat untuk bulan ini.",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spHardwareContent.Children.Add(emptyCard);
                return;
            }

            Border hwCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel hwSp = new StackPanel();

            for (int i = 0; i < sortedHw.Count; i++)
            {
                var h = sortedHw[i];
                double pct = totalHwKwh > 0 ? (h.Kwh / totalHwKwh) * 100.0 : 0;
                double avgDailyCost = h.Cost / daysCount;

                Grid row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
                row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Grid topGrid = new Grid();
                topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });
                topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                StackPanel nameSp = new StackPanel { Orientation = Orientation.Horizontal };
                nameSp.Children.Add(new TextBlock
                {
                    Text = h.Name,
                    FontSize = 12.5,
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                });
                nameSp.Children.Add(new TextBlock
                {
                    Text = string.Format(" ({0:F1}%)", pct),
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    Margin = new Thickness(6, 1, 0, 0)
                });
                Grid.SetColumn(nameSp, 0);
                topGrid.Children.Add(nameSp);

                StackPanel valSp = new StackPanel { Orientation = Orientation.Horizontal };
                valSp.Children.Add(new TextBlock
                {
                    Text = string.Format(CultureInfo.InvariantCulture, "{0:F4} kWh", h.Kwh),
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    Margin = new Thickness(0, 0, 12, 0)
                });
                valSp.Children.Add(new TextBlock
                {
                    Text = string.Format("Rp {0:N2}", h.Cost),
                    FontSize = 12.5,
                    FontFamily = new FontFamily("Consolas"),
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128))
                });
                Grid.SetColumn(valSp, 1);
                topGrid.Children.Add(valSp);

                Grid.SetRow(topGrid, 0);
                row.Children.Add(topGrid);

                ProgressBar pb = new ProgressBar
                {
                    Height = 5,
                    Minimum = 0,
                    Maximum = 100,
                    Value = pct,
                    Foreground = GetHwProgressBrush(i),
                    Background = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(0, 6, 0, 2)
                };
                Grid.SetRow(pb, 1);
                row.Children.Add(pb);

                hwSp.Children.Add(row);

                if (i < sortedHw.Count - 1)
                {
                    hwSp.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Color.FromArgb(40, 51, 65, 85)), Margin = new Thickness(0, 4, 0, 4) });
                }
            }

            hwCard.Child = hwSp;
            spHardwareContent.Children.Add(hwCard);
        }

        private void RenderInfoTab()
        {
            spInfoContent.Children.Clear();
            LiveSessionSnapshot live = (liveDataProvider != null) ? liveDataProvider() : null;

            // SECTION 1: STATUS SESI SAAT INI (LIVE)
            TextBlock sec1Title = new TextBlock
            {
                Text = "STATUS SESI AKTIF (LIVE ENGINE)",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 0, 0, 8)
            };
            spInfoContent.Children.Add(sec1Title);

            Border liveCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel liveSp = new StackPanel();

            string statusText = "🟢 AKTIF BERJALAN";
            string startTimeStr = live != null ? live.SessionStartTime.ToString("dd MMMM yyyy, HH:mm:ss") : DateTime.Now.ToString("dd MMMM yyyy, HH:mm:ss");
            
            TimeSpan liveDur = (live != null) ? TimeSpan.FromHours(live.ActiveRunningHours) : TimeSpan.Zero;
            string liveDurStr = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)liveDur.TotalHours, liveDur.Minutes, liveDur.Seconds);
            string liveWattStr = (live != null) ? string.Format("{0:F1} Watt", live.CurrentWatt) : "0.0 Watt";
            string liveEnergyStr = (live != null) ? string.Format(CultureInfo.InvariantCulture, "{0:F4} kWh", live.AccumulatedKwh) : "0.0000 kWh";
            string liveCostStr = (live != null) ? string.Format("Rp {0:N2}", live.AccumulatedKwh * Config.Tariff) : "Rp 0,00";

            liveSp.Children.Add(CreateInfoRow("Status Background Engine", statusText, new SolidColorBrush(Color.FromRgb(74, 222, 128))));
            liveSp.Children.Add(CreateInfoRow("Waktu Mulai Sesi", startTimeStr, Brushes.White));
            liveSp.Children.Add(CreateInfoRow("Durasi Berjalan Sesi Ini", liveDurStr, new SolidColorBrush(Color.FromRgb(56, 189, 248))));
            liveSp.Children.Add(CreateInfoRow("Konsumsi Daya Saat Ini", liveWattStr, new SolidColorBrush(Color.FromRgb(245, 158, 11))));
            liveSp.Children.Add(CreateInfoRow("Total Energi Sesi Ini", liveEnergyStr, new SolidColorBrush(Color.FromRgb(56, 189, 248))));
            liveSp.Children.Add(CreateInfoRow("Estimasi Biaya Sesi Ini", liveCostStr, new SolidColorBrush(Color.FromRgb(74, 222, 128))));

            liveCard.Child = liveSp;
            spInfoContent.Children.Add(liveCard);

            // SECTION 2: PARAMETER KONFIGURASI (config.txt)
            TextBlock sec2Title = new TextBlock
            {
                Text = "PARAMETER & PENGATURAN (CONFIG.TXT)",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 0, 0, 8)
            };
            spInfoContent.Children.Add(sec2Title);

            Border cfgCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel cfgSp = new StackPanel();
            cfgSp.Children.Add(CreateInfoRow("Tarif Listrik PLN", string.Format("Rp {0:N2} / kWh", Config.Tariff), Brushes.White));
            cfgSp.Children.Add(CreateInfoRow("Estimasi Jam Aktif Harian", string.Format("{0} Jam / hari", Config.HoursPerDay), Brushes.White));
            cfgSp.Children.Add(CreateInfoRow("Estimasi Jam Aktif Bulanan", string.Format("{0} Jam / bulan", Config.HoursPerMonth), Brushes.White));
            cfgSp.Children.Add(CreateInfoRow("Interval Autosave Laporan", string.Format("{0:F2} Menit (~5 detik proteksi mati lampu)", Config.AutosaveMinutes), Brushes.White));
            cfgSp.Children.Add(CreateInfoRow("Kompensasi Watt Tetap (Offset)", string.Format("+{0:F1} Watt", Config.WattOffset), Brushes.White));
            cfgSp.Children.Add(CreateInfoRow("Interval Refresh Sensor", string.Format("{0} ms", Config.RefreshRateMs), Brushes.White));

            cfgCard.Child = cfgSp;
            spInfoContent.Children.Add(cfgCard);

            // SECTION 3: PERIFERAL STATIS & HARDWARE
            TextBlock sec3Title = new TextBlock
            {
                Text = "PERIFERAL STATIS & SPESIFIKASI",
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(2, 0, 0, 8)
            };
            spInfoContent.Children.Add(sec3Title);

            Border hwCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 16)
            };

            StackPanel hwInfoSp = new StackPanel();
            hwInfoSp.Children.Add(CreateInfoRow("Mesin Pemindai", "LibreHardwareMonitorLib (Kernel Sensor Driver)", new SolidColorBrush(Color.FromRgb(56, 189, 248))));
            hwInfoSp.Children.Add(CreateInfoRow("Arsitektur Antarmuka", "Native 64-bit WPF (.NET Framework 4.7.2)", Brushes.White));

            foreach (var st in Config.StaticPeripherals)
            {
                hwInfoSp.Children.Add(CreateInfoRow("Periferal: " + st.Key, string.Format("{0:F1} Watt", st.Value), new SolidColorBrush(Color.FromRgb(245, 158, 11))));
            }

            hwCard.Child = hwInfoSp;
            spInfoContent.Children.Add(hwCard);
        }

        private Grid CreateInfoRow(string label, string value, Brush valBrush)
        {
            Grid g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            TextBlock lbl = new TextBlock
            {
                Text = label,
                FontSize = 11.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            };
            Grid.SetColumn(lbl, 0);
            g.Children.Add(lbl);

            TextBlock val = new TextBlock
            {
                Text = value,
                FontSize = 11.5,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.SemiBold,
                Foreground = valBrush
            };
            Grid.SetColumn(val, 1);
            g.Children.Add(val);

            return g;
        }

        private Border CreateStatCard(string label, string value, string subtext, Color accent)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(90, 30, 41, 59)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8)
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 9.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(accent)
            });
            sp.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 16,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 3, 0, 2)
            });
            sp.Children.Add(new TextBlock
            {
                Text = subtext,
                FontSize = 9.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184))
            });

            b.Child = sp;
            return b;
        }

        private TextBlock CreateThText(string text, int col, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                HorizontalAlignment = align
            };
            Grid.SetColumn(tb, col);
            return tb;
        }

        private Brush GetHwProgressBrush(int index)
        {
            switch (index % 5)
            {
                case 0: return new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Sky
                case 1: return new SolidColorBrush(Color.FromRgb(168, 85, 247)); // Purple
                case 2: return new SolidColorBrush(Color.FromRgb(74, 222, 128)); // Emerald
                case 3: return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Amber
                default: return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            }
        }

        private bool IsInsideElement(DependencyObject source, DependencyObject target)
        {
            if (source == null || target == null) return false;
            DependencyObject current = source;
            while (current != null)
            {
                if (current == target) return true;
                if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                {
                    current = VisualTreeHelper.GetParent(current);
                }
                else
                {
                    current = LogicalTreeHelper.GetParent(current);
                }
            }
            return false;
        }

        private void OpenCurrentMdFile()
        {
            string path = currentTab == 0 ? ReportGenerator.GetReportPath(selectedMonth) : ReportGenerator.GetHardwareReportPath(selectedMonth);
            if (File.Exists(path))
            {
                try { Process.Start(path); } catch { }
            }
            else
            {
                MessageBox.Show("Berkas laporan Markdown belum tersedia untuk bulan ini.", "WattMonitor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
