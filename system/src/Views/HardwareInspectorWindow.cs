using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;

namespace WattMonitor
{
    public enum SortColumn
    {
        Default,
        Name,
        Current,
        Minimum,
        Maximum,
        Average
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public class SensorStats
    {
        public double Current;
        public double Min = double.MaxValue;
        public double Max = double.MinValue;
        public double Sum = 0;
        public int Count = 0;

        public double Avg
        {
            get { return Count > 0 ? (Sum / Count) : Current; }
        }

        public void Update(double val)
        {
            Current = val;
            if (val < Min) Min = val;
            if (val > Max) Max = val;
            Sum += val;
            Count++;
        }

        public void Reset()
        {
            Min = Current;
            Max = Current;
            Sum = Current;
            Count = 1;
        }
    }

    public class SensorRowBinding
    {
        public ISensor Sensor;
        public SensorStats Stats;
        public Border RowBorder;
        public TextBlock TxtCurrent;
        public TextBlock TxtMin;
        public TextBlock TxtMax;
        public TextBlock TxtAvg;
        public int OriginalIndex;
    }

    public class HardwareGroupSection
    {
        public IHardware Hardware;
        public Border GroupCard;
        public Border GroupHeader;
        public ContentControl ChevronHost;
        public StackPanel SensorsSp;
        public List<SensorRowBinding> RowBindings = new List<SensorRowBinding>();
        public bool IsExpanded = true;

        public void ToggleExpanded()
        {
            SetExpanded(!IsExpanded);
        }

        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded;
            if (SensorsSp != null)
            {
                SensorsSp.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            }
            if (ChevronHost != null)
            {
                ChevronHost.Content = LucideIcon.Create(expanded ? LucideIcon.ChevronDown : LucideIcon.ChevronRight, 13, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 2.0);
            }
            if (GroupHeader != null)
            {
                GroupHeader.CornerRadius = expanded ? new CornerRadius(5, 5, 0, 0) : new CornerRadius(5);
            }
        }

        public void ApplySorting(SortColumn col, SortDirection dir)
        {
            if (RowBindings == null || SensorsSp == null) return;

            if (col == SortColumn.Default)
            {
                RowBindings.Sort((a, b) => a.OriginalIndex.CompareTo(b.OriginalIndex));
            }
            else if (col == SortColumn.Name)
            {
                RowBindings.Sort((a, b) =>
                {
                    int c = string.Compare(a.Sensor.Name, b.Sensor.Name, StringComparison.OrdinalIgnoreCase);
                    return dir == SortDirection.Descending ? -c : c;
                });
            }
            else
            {
                RowBindings.Sort((a, b) =>
                {
                    double valA = GetSortValue(a, col);
                    double valB = GetSortValue(b, col);
                    int c = valA.CompareTo(valB);
                    return dir == SortDirection.Descending ? -c : c;
                });
            }

            SensorsSp.Children.Clear();
            for (int i = 0; i < RowBindings.Count; i++)
            {
                var b = RowBindings[i];
                b.RowBorder.Background = i % 2 == 0 
                    ? new SolidColorBrush(Color.FromArgb(40, 15, 23, 42)) 
                    : Brushes.Transparent;
                SensorsSp.Children.Add(b.RowBorder);
            }
        }

        private static double GetSortValue(SensorRowBinding b, SortColumn col)
        {
            if (b.Stats == null) return double.MinValue;
            switch (col)
            {
                case SortColumn.Current: return b.Stats.Current;
                case SortColumn.Minimum: return b.Stats.Min == double.MaxValue ? double.MinValue : b.Stats.Min;
                case SortColumn.Maximum: return b.Stats.Max == double.MinValue ? double.MinValue : b.Stats.Max;
                case SortColumn.Average: return b.Stats.Avg;
                default: return 0;
            }
        }
    }

    public class HardwareInspectorWindow : Window
    {
        private HardwareScanner scanner;
        private Action onOpenSettings;
        private DispatcherTimer updateTimer;

        private List<HardwareGroupSection> hardwareGroups = new List<HardwareGroupSection>();
        private List<SensorRowBinding> bindings = new List<SensorRowBinding>();
        private StackPanel spSensorsList;
        private TextBlock txtSummaryPower;
        private TextBlock txtSummaryMaxTemp;

        private SortColumn currentSortColumn = SortColumn.Default;
        private SortDirection currentSortDirection = SortDirection.Descending;
        private bool allExpandedState = true;

        private TextBlock txtColName;
        private TextBlock txtColCurrent;
        private TextBlock txtColMin;
        private TextBlock txtColMax;
        private TextBlock txtColAvg;

        public HardwareInspectorWindow(HardwareScanner scanner = null, Action onOpenSettings = null)
        {
            this.Title = "Sensor Hardware - WattMonitor";
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 660;
            this.Height = 700;
            this.MinWidth = 540;
            this.MinHeight = 480;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanResizeWithGrip;

            this.scanner = scanner;
            this.onOpenSettings = onOpenSettings;

            ModernDialog.ApplyDarkToolTipStyle(this);
            ApplyModernScrollbarStyle();
            BuildUI();

            // Live Update Timer (ticks every 1000ms)
            updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
            updateTimer.Tick += (s, e) => RefreshLiveSensors();
            updateTimer.Start();

            this.Closed += (s, e) =>
            {
                if (updateTimer != null)
                {
                    updateTimer.Stop();
                    updateTimer = null;
                }
            };

            RefreshLiveSensors();
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
            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)), // Slate 950
                BorderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(10)
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Toolbar / Summary
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Table Columns Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3: Sensors List
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Footer

            // 0. HEADER BAR
            Border header = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 10, 12, 10),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };
            header.MouseDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };

            Grid hGrid = new Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconPulse = LucideIcon.Create(LucideIcon.Activity, 16, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false, 2.0);
            iconPulse.Margin = new Thickness(0, 0, 8, 0);
            titleSp.Children.Add(iconPulse);

            TextBlock titleTb = new TextBlock
            {
                Text = "Sensor Hardware (Live Telemetry)",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleSp.Children.Add(titleTb);
            Grid.SetColumn(titleSp, 0);
            hGrid.Children.Add(titleSp);

            StackPanel winBtns = new StackPanel { Orientation = Orientation.Horizontal };
            winBtns.Children.Add(CreateHeaderButton(LucideIcon.Minus, "Minimize", () => this.WindowState = WindowState.Minimized));
            winBtns.Children.Add(CreateHeaderButton(LucideIcon.X, "Tutup", () => this.Close()));
            Grid.SetColumn(winBtns, 1);
            hGrid.Children.Add(winBtns);

            header.Child = hGrid;
            Grid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // 1. TOOLBAR / SUMMARY BAR
            Border toolBar = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 8, 14, 8)
            };

            Grid tGrid = new Grid();
            tGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel statsSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Live Total Power badge
            statsSp.Children.Add(LucideIcon.Create(LucideIcon.Zap, 13, new SolidColorBrush(Color.FromRgb(245, 158, 11)), true));
            txtSummaryPower = new TextBlock
            {
                Text = " Power: 0.0 W",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 14, 0)
            };
            statsSp.Children.Add(txtSummaryPower);

            // Max Temp badge
            statsSp.Children.Add(LucideIcon.Create(LucideIcon.Thermometer, 13, new SolidColorBrush(Color.FromRgb(239, 68, 68)), false));
            txtSummaryMaxTemp = new TextBlock
            {
                Text = " Max Temp: --°C",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(251, 146, 60)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 14, 0)
            };
            statsSp.Children.Add(txtSummaryMaxTemp);
            Grid.SetColumn(statsSp, 0);
            tGrid.Children.Add(statsSp);

            // Action Buttons
            StackPanel actSp = new StackPanel { Orientation = Orientation.Horizontal };

            Button btnToggleExpand = CreateHeaderButton(LucideIcon.Layers, "Bentangkan / Ciutkan Semua Grup (Expand/Collapse All)", () => ToggleExpandAll());
            actSp.Children.Add(btnToggleExpand);

            Button btnResetMinMax = CreateHeaderButton(LucideIcon.RotateCcw, "Reset Min/Max (Hitung ulang batas terendah & tertinggi)", () => ResetAllMinMax());
            btnResetMinMax.Margin = new Thickness(4, 0, 0, 0);
            actSp.Children.Add(btnResetMinMax);

            if (onOpenSettings != null)
            {
                Button btnSettings = CreateHeaderButton(LucideIcon.Settings, "Kelola Komponen & Daya", () => onOpenSettings());
                btnSettings.Margin = new Thickness(4, 0, 0, 0);
                actSp.Children.Add(btnSettings);
            }

            Grid.SetColumn(actSp, 1);
            tGrid.Children.Add(actSp);

            toolBar.Child = tGrid;
            Grid.SetRow(toolBar, 1);
            mainGrid.Children.Add(toolBar);

            // 2. TABLE COLUMNS HEADER (CLICKABLE SORT)
            Border colHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 14, 5)
            };

            Grid cGrid = new Grid();
            cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Sensor Name
            cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85, GridUnitType.Pixel) }); // Current
            cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70, GridUnitType.Pixel) }); // Min
            cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70, GridUnitType.Pixel) }); // Max
            cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70, GridUnitType.Pixel) }); // Avg

            cGrid.Children.Add(CreateClickableColumnHeader("SENSOR / HARDWARE", SortColumn.Name, HorizontalAlignment.Left, 0, out txtColName));
            cGrid.Children.Add(CreateClickableColumnHeader("CURRENT", SortColumn.Current, HorizontalAlignment.Right, 1, out txtColCurrent));
            cGrid.Children.Add(CreateClickableColumnHeader("MINIMUM", SortColumn.Minimum, HorizontalAlignment.Right, 2, out txtColMin));
            cGrid.Children.Add(CreateClickableColumnHeader("MAXIMUM", SortColumn.Maximum, HorizontalAlignment.Right, 3, out txtColMax));
            cGrid.Children.Add(CreateClickableColumnHeader("AVERAGE", SortColumn.Average, HorizontalAlignment.Right, 4, out txtColAvg));

            colHeader.Child = cGrid;
            Grid.SetRow(colHeader, 2);
            mainGrid.Children.Add(colHeader);

            // 3. SENSORS LIST SCROLLVIEWER
            spSensorsList = new StackPanel { Margin = new Thickness(6, 4, 6, 6) };
            ScrollViewer sv = new ScrollViewer
            {
                Content = spSensorsList,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            Grid.SetRow(sv, 3);
            mainGrid.Children.Add(sv);

            // 4. FOOTER
            Border footer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 51, 65, 85)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                CornerRadius = new CornerRadius(0, 0, 10, 10)
            };

            Grid fGrid = new Grid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock fBadge = new TextBlock
            {
                Text = "⚡ Powered by LibreHardwareMonitor Ring-0 Driver Telemetry",
                FontSize = 10.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(fBadge, 0);
            fGrid.Children.Add(fBadge);

            Button btnRebuild = CreateHeaderButton(LucideIcon.RotateCcw, "Pindai Ulang Seluruh Sensor", () => RebuildSensorTree());
            Grid.SetColumn(btnRebuild, 1);
            fGrid.Children.Add(btnRebuild);

            footer.Child = fGrid;
            Grid.SetRow(footer, 4);
            mainGrid.Children.Add(footer);

            rootBorder.Child = mainGrid;
            this.Content = rootBorder;

            RebuildSensorTree();
        }

        private Border CreateClickableColumnHeader(string text, SortColumn col, HorizontalAlignment align, int colIndex, out TextBlock txtLabel)
        {
            Border b = new Border
            {
                Background = Brushes.Transparent,
                Padding = new Thickness(6, 3, 6, 3),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                ToolTip = string.Format("Klik untuk mengurutkan (Sort) berdasarkan {0}", text)
            };

            txtLabel = new TextBlock
            {
                Text = text,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                HorizontalAlignment = align,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = txtLabel;

            b.MouseEnter += (s, e) => { b.Background = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)); };
            b.MouseLeave += (s, e) => { b.Background = Brushes.Transparent; };

            SortColumn capturedCol = col;
            b.MouseLeftButtonUp += (s, e) => OnColumnHeaderClicked(capturedCol);

            Grid.SetColumn(b, colIndex);
            return b;
        }

        private void OnColumnHeaderClicked(SortColumn col)
        {
            if (currentSortColumn == col)
            {
                if (currentSortDirection == SortDirection.Descending)
                {
                    currentSortDirection = SortDirection.Ascending;
                }
                else
                {
                    currentSortColumn = SortColumn.Default;
                    currentSortDirection = SortDirection.Descending;
                }
            }
            else
            {
                currentSortColumn = col;
                currentSortDirection = SortDirection.Descending;
            }

            UpdateColumnHeaderLabels();
            ApplySortingToAllGroups();
        }

        private void UpdateColumnHeaderLabels()
        {
            SetHeaderLabel(txtColName, "SENSOR / HARDWARE", SortColumn.Name);
            SetHeaderLabel(txtColCurrent, "CURRENT", SortColumn.Current);
            SetHeaderLabel(txtColMin, "MINIMUM", SortColumn.Minimum);
            SetHeaderLabel(txtColMax, "MAXIMUM", SortColumn.Maximum);
            SetHeaderLabel(txtColAvg, "AVERAGE", SortColumn.Average);
        }

        private void SetHeaderLabel(TextBlock tb, string baseTitle, SortColumn col)
        {
            if (tb == null) return;
            if (currentSortColumn == col)
            {
                string arrow = currentSortDirection == SortDirection.Descending ? " ▼" : " ▲";
                tb.Text = baseTitle + arrow;
                tb.Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Sky 400
            }
            else
            {
                tb.Text = baseTitle;
                tb.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)); // Slate 400
            }
        }

        private void ApplySortingToAllGroups()
        {
            foreach (var grp in hardwareGroups)
            {
                grp.ApplySorting(currentSortColumn, currentSortDirection);
            }
        }

        private void ToggleExpandAll()
        {
            allExpandedState = !allExpandedState;
            foreach (var grp in hardwareGroups)
            {
                grp.SetExpanded(allExpandedState);
            }
        }

        public void RebuildSensorTree()
        {
            if (scanner == null || spSensorsList == null) return;
            spSensorsList.Children.Clear();
            hardwareGroups.Clear();
            bindings.Clear();

            var hardwares = scanner.GetHardwareList();
            if (hardwares == null || hardwares.Count == 0)
            {
                TextBlock emptyTb = new TextBlock
                {
                    Text = "Tidak ada hardware yang terdeteksi. Pastikan aplikasi dijalankan dengan hak administrator.",
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontSize = 11,
                    Margin = new Thickness(14),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spSensorsList.Children.Add(emptyTb);
                return;
            }

            foreach (var hw in hardwares)
            {
                BuildHardwareSection(hw);
            }

            if (currentSortColumn != SortColumn.Default)
            {
                ApplySortingToAllGroups();
            }
        }

        private void BuildHardwareSection(IHardware hw)
        {
            try { hw.Update(); } catch { }

            // Kumpulkan semua sensor termasuk sub-hardware
            var allSensors = new List<ISensor>();
            Action<IHardware> collect = null;
            collect = (h) =>
            {
                allSensors.AddRange(h.Sensors);
                foreach (var sub in h.SubHardware)
                {
                    try { sub.Update(); } catch { }
                    collect(sub);
                }
            };
            collect(hw);

            if (allSensors.Count == 0) return;

            var group = new HardwareGroupSection
            {
                Hardware = hw,
                IsExpanded = true
            };

            // Header Group Card
            Border groupCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Margin = new Thickness(0, 0, 0, 8)
            };
            group.GroupCard = groupCard;

            StackPanel groupSp = new StackPanel();

            // Device Title Bar (Click to Expand / Collapse)
            Border groupHeader = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)), // Slate 800
                Padding = new Thickness(10, 7, 10, 7),
                CornerRadius = new CornerRadius(5, 5, 0, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Klik untuk menciutkan / membentangkan grup (Expand/Collapse)"
            };
            group.GroupHeader = groupHeader;

            Grid ghGrid = new Grid();
            ghGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 0: Chevron
            ghGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 1: Icon
            ghGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Col 2: Name
            ghGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Col 3: Count Badge

            // Col 0: Chevron Host
            ContentControl chevronHost = new ContentControl
            {
                Content = LucideIcon.Create(LucideIcon.ChevronDown, 13, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 2.0),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Focusable = false,
                IsHitTestVisible = false
            };
            group.ChevronHost = chevronHost;
            Grid.SetColumn(chevronHost, 0);
            ghGrid.Children.Add(chevronHost);

            // Col 1: Hardware Icon
            string iconSvg = GetHardwareIcon(hw.HardwareType);
            var iconElem = LucideIcon.Create(iconSvg, 13, new SolidColorBrush(Color.FromRgb(56, 189, 248)), false, 2.0);
            iconElem.Margin = new Thickness(0, 0, 8, 0);
            iconElem.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(iconElem, 1);
            ghGrid.Children.Add(iconElem);

            // Col 2: Hardware Name & Type
            TextBlock hwNameTb = new TextBlock
            {
                Text = string.Format("{0} [{1}]", hw.Name, hw.HardwareType),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(hwNameTb, 2);
            ghGrid.Children.Add(hwNameTb);

            // Col 3: Sensor Count Badge
            TextBlock countTb = new TextBlock
            {
                Text = string.Format("{0} sensor", allSensors.Count),
                FontSize = 10,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 2, 0)
            };
            Grid.SetColumn(countTb, 3);
            ghGrid.Children.Add(countTb);

            groupHeader.Child = ghGrid;

            // Hover effects & Click to toggle expand
            groupHeader.MouseEnter += (s, e) => groupHeader.Background = new SolidColorBrush(Color.FromRgb(51, 65, 85));
            groupHeader.MouseLeave += (s, e) => groupHeader.Background = new SolidColorBrush(Color.FromRgb(30, 41, 59));
            HardwareGroupSection capturedGroup = group;
            groupHeader.MouseLeftButtonUp += (s, e) => capturedGroup.ToggleExpanded();

            groupSp.Children.Add(groupHeader);

            // Sensors Container
            StackPanel sensorsSp = new StackPanel { Margin = new Thickness(6, 4, 6, 6) };
            group.SensorsSp = sensorsSp;

            int originalIdx = 0;
            foreach (var s in allSensors)
            {
                if (s.SensorType == SensorType.Factor) continue;

                var binding = new SensorRowBinding
                {
                    Sensor = s,
                    Stats = new SensorStats(),
                    OriginalIndex = originalIdx
                };

                Border rowBorder = new Border
                {
                    Background = originalIdx % 2 == 0 
                        ? new SolidColorBrush(Color.FromArgb(40, 15, 23, 42)) 
                        : Brushes.Transparent,
                    Padding = new Thickness(6, 3, 6, 3),
                    CornerRadius = new CornerRadius(3)
                };
                binding.RowBorder = rowBorder;

                Grid rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85, GridUnitType.Pixel) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70, GridUnitType.Pixel) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70, GridUnitType.Pixel) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70, GridUnitType.Pixel) });

                // Col 0: Sensor Name
                TextBlock txtName = new TextBlock
                {
                    Text = s.Name,
                    FontSize = 11,
                    FontFamily = new FontFamily("Segoe UI, Inter"),
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    ToolTip = string.Format("{0} ({1})", s.Name, s.SensorType)
                };
                Grid.SetColumn(txtName, 0);
                rowGrid.Children.Add(txtName);

                // Col 1: Current
                binding.TxtCurrent = CreateSensorValText(FontWeights.Bold, GetSensorBrush(s.SensorType));
                Grid.SetColumn(binding.TxtCurrent, 1);
                rowGrid.Children.Add(binding.TxtCurrent);

                // Col 2: Min
                binding.TxtMin = CreateSensorValText(FontWeights.Normal, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
                Grid.SetColumn(binding.TxtMin, 2);
                rowGrid.Children.Add(binding.TxtMin);

                // Col 3: Max
                binding.TxtMax = CreateSensorValText(FontWeights.Normal, new SolidColorBrush(Color.FromRgb(251, 146, 60)));
                Grid.SetColumn(binding.TxtMax, 3);
                rowGrid.Children.Add(binding.TxtMax);

                // Col 4: Avg
                binding.TxtAvg = CreateSensorValText(FontWeights.Normal, new SolidColorBrush(Color.FromRgb(148, 163, 184)));
                Grid.SetColumn(binding.TxtAvg, 4);
                rowGrid.Children.Add(binding.TxtAvg);

                rowBorder.Child = rowGrid;
                sensorsSp.Children.Add(rowBorder);
                group.RowBindings.Add(binding);
                bindings.Add(binding);
                originalIdx++;
            }

            groupSp.Children.Add(sensorsSp);
            groupCard.Child = groupSp;
            spSensorsList.Children.Add(groupCard);
            hardwareGroups.Add(group);
        }

        private TextBlock CreateSensorValText(FontWeight weight, Brush brush)
        {
            return new TextBlock
            {
                Text = "-",
                FontSize = 11,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = weight,
                Foreground = brush,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private Brush GetSensorBrush(SensorType type)
        {
            switch (type)
            {
                case SensorType.Power: return new SolidColorBrush(Color.FromRgb(56, 189, 248)); // Cyan
                case SensorType.Temperature: return new SolidColorBrush(Color.FromRgb(251, 146, 60)); // Orange
                case SensorType.Load: return new SolidColorBrush(Color.FromRgb(52, 211, 153)); // Emerald
                case SensorType.Clock: return new SolidColorBrush(Color.FromRgb(192, 132, 252)); // Purple
                case SensorType.Voltage: return new SolidColorBrush(Color.FromRgb(250, 204, 21)); // Yellow
                case SensorType.Fan: return new SolidColorBrush(Color.FromRgb(125, 211, 252)); // Sky
                default: return Brushes.White;
            }
        }

        private string GetHardwareIcon(HardwareType type)
        {
            switch (type)
            {
                case HardwareType.Cpu: return LucideIcon.Cpu;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel: return LucideIcon.Activity;
                case HardwareType.Storage: return LucideIcon.HardDrive;
                case HardwareType.Memory: return LucideIcon.Layers;
                case HardwareType.Motherboard: return LucideIcon.Grid;
                default: return LucideIcon.Monitor;
            }
        }

        private void RefreshLiveSensors()
        {
            if (scanner == null || bindings.Count == 0) return;

            double totalPower = 0;
            double maxTemp = -100;

            foreach (var b in bindings)
            {
                var s = b.Sensor;
                if (!s.Value.HasValue) continue;

                double val = s.Value.Value;
                b.Stats.Update(val);

                string curStr = FormatSensorValue(s.SensorType, b.Stats.Current);
                string minStr = FormatSensorValue(s.SensorType, b.Stats.Min);
                string maxStr = FormatSensorValue(s.SensorType, b.Stats.Max);
                string avgStr = FormatSensorValue(s.SensorType, b.Stats.Avg);

                b.TxtCurrent.Text = curStr;
                b.TxtMin.Text = minStr;
                b.TxtMax.Text = maxStr;
                b.TxtAvg.Text = avgStr;

                if (s.SensorType == SensorType.Power)
                {
                    string sName = s.Name.ToLower();
                    if (sName.Contains("package") || sName.Contains("total") || sName.Contains("core"))
                    {
                        totalPower += val;
                    }
                }
                else if (s.SensorType == SensorType.Temperature)
                {
                    if (val > maxTemp) maxTemp = val;
                }
            }

            if (txtSummaryPower != null)
            {
                txtSummaryPower.Text = string.Format(" Power: {0:0.#} W", totalPower);
            }
            if (txtSummaryMaxTemp != null)
            {
                txtSummaryMaxTemp.Text = maxTemp > -50 ? string.Format(" Max Temp: {0:0}°C", maxTemp) : " Max Temp: --°C";
            }
        }

        private string FormatSensorValue(SensorType type, double val)
        {
            switch (type)
            {
                case SensorType.Power: return string.Format("{0:0.#} W", val);
                case SensorType.Temperature: return string.Format("{0:0.#} °C", val);
                case SensorType.Load: return string.Format("{0:0.#} %", val);
                case SensorType.Clock: return string.Format("{0:0} MHz", val);
                case SensorType.Fan: return string.Format("{0:0} RPM", val);
                case SensorType.Voltage: return string.Format("{0:0.##} V", val);
                case SensorType.Data: return string.Format("{0:0.##} GB", val);
                case SensorType.SmallData: return string.Format("{0:0.#} MB", val);
                case SensorType.Throughput:
                    return val >= (1024 * 1024) 
                        ? string.Format("{0:0.#} MB/s", val / (1024.0 * 1024.0)) 
                        : string.Format("{0:0.#} KB/s", val / 1024.0);
                default:
                    return string.Format("{0:0.##}", val);
            }
        }

        private void ResetAllMinMax()
        {
            foreach (var b in bindings)
            {
                b.Stats.Reset();
            }
            RefreshLiveSensors();
        }

        private Button CreateHeaderButton(string iconPath, string tooltip, Action onClick)
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
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
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
            btn.Content = LucideIcon.Create(iconPath, 13, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 1.8);
            btn.Click += (s, e) => onClick();
            return btn;
        }
    }
}
