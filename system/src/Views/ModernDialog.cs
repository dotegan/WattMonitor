using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WattMonitor
{
    public class ModernDialog : Window
    {
        private bool dialogResultValue = false;

        public ModernDialog(string title, string message, bool isConfirm = false, bool isError = false, Window owner = null)
        {
            this.Title = title;
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = Brushes.Transparent;
            this.Width = 380;
            this.SizeToContent = SizeToContent.Height;
            this.ResizeMode = ResizeMode.NoResize;
            this.ShowInTaskbar = false;

            if (owner != null && owner.IsLoaded && owner.IsVisible)
            {
                this.Owner = owner;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            BuildUI(title, message, isConfirm, isError);
        }

        private void BuildUI(string title, string message, bool isConfirm, bool isError)
        {
            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(11, 15, 25)), // Slate 950
                BorderBrush = new SolidColorBrush(isError ? Color.FromRgb(239, 68, 68) : Color.FromRgb(51, 65, 85)), // Red or Slate 700
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(18, 16, 18, 16)
            };

            rootBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                    this.DragMove();
            };

            StackPanel mainSp = new StackPanel();

            // 1. Header (Icon + Title + Close Button)
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Color accentColor = isError 
                ? Color.FromRgb(239, 68, 68) 
                : (isConfirm ? Color.FromRgb(56, 189, 248) : Color.FromRgb(245, 158, 11));

            string iconSvg = isError 
                ? LucideIcon.X 
                : (isConfirm ? LucideIcon.HelpCircle : LucideIcon.Info);

            var iconElem = LucideIcon.Create(iconSvg, 16, new SolidColorBrush(accentColor), false, 2.0);
            iconElem.Margin = new Thickness(0, 0, 8, 0);
            iconElem.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(iconElem, 0);
            headerGrid.Children.Add(iconElem);

            TextBlock titleTb = new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleTb, 1);
            headerGrid.Children.Add(titleTb);

            Button btnClose = new Button
            {
                Width = 24,
                Height = 24,
                Cursor = Cursors.Hand,
                Focusable = false
            };
            ControlTemplate ct = new ControlTemplate(typeof(Button));
            FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            bd.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            ct.VisualTree = bd;
            btnClose.Template = ct;
            btnClose.Content = LucideIcon.Create(LucideIcon.X, 12, new SolidColorBrush(Color.FromRgb(148, 163, 184)), false, 2.0);
            btnClose.Click += (s, e) =>
            {
                this.dialogResultValue = false;
                this.Close();
            };
            Grid.SetColumn(btnClose, 2);
            headerGrid.Children.Add(btnClose);

            mainSp.Children.Add(headerGrid);

            // 2. Message Body
            TextBlock msgTb = new TextBlock
            {
                Text = message,
                FontSize = 11.5,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)), // Slate 200
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18,
                Margin = new Thickness(2, 0, 2, 18)
            };
            mainSp.Children.Add(msgTb);

            // 3. Actions Row
            StackPanel btnSp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (isConfirm)
            {
                Button btnCancel = CreateButton("Batal", false, () =>
                {
                    this.dialogResultValue = false;
                    this.Close();
                });
                btnCancel.Margin = new Thickness(0, 0, 8, 0);
                btnSp.Children.Add(btnCancel);

                Button btnOk = CreateButton("Ya, Lanjutkan", true, () =>
                {
                    this.dialogResultValue = true;
                    this.Close();
                });
                btnSp.Children.Add(btnOk);
            }
            else
            {
                Button btnOk = CreateButton("Mengerti", true, () =>
                {
                    this.dialogResultValue = true;
                    this.Close();
                });
                btnSp.Children.Add(btnOk);
            }

            mainSp.Children.Add(btnSp);
            rootBorder.Child = mainSp;
            this.Content = rootBorder;
        }

        private Button CreateButton(string text, bool isPrimary, Action onClick)
        {
            Button btn = new Button
            {
                Content = text,
                Height = 28,
                Padding = new Thickness(14, 0, 14, 0),
                Cursor = Cursors.Hand,
                FontFamily = new FontFamily("Segoe UI, Inter"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = isPrimary ? Brushes.White : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Focusable = false
            };

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border), "bd");
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, isPrimary 
                ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) 
                : Brushes.Transparent);
            border.SetValue(Border.BorderBrushProperty, isPrimary 
                ? new SolidColorBrush(Color.FromRgb(51, 65, 85)) 
                : new SolidColorBrush(Color.FromArgb(80, 51, 65, 85)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;

            Trigger hoverTrigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, isPrimary 
                ? new SolidColorBrush(Color.FromRgb(56, 189, 248)) 
                : new SolidColorBrush(Color.FromArgb(120, 51, 65, 85)), "bd"));
            hoverTrigger.Setters.Add(new Setter(Button.ForegroundProperty, isPrimary ? Brushes.Black : Brushes.White));
            template.Triggers.Add(hoverTrigger);

            btn.Template = template;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        // ================= STATIC HELPERS =================
        public static void Show(string title, string message, Window owner = null, bool isError = false)
        {
            try
            {
                var dlg = new ModernDialog(title, message, false, isError, owner);
                dlg.ShowDialog();
            }
            catch
            {
                MessageBox.Show(message, title);
            }
        }

        public static bool ShowConfirm(string title, string message, Window owner = null)
        {
            try
            {
                var dlg = new ModernDialog(title, message, true, false, owner);
                dlg.ShowDialog();
                return dlg.dialogResultValue;
            }
            catch
            {
                return MessageBox.Show(message, title, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            }
        }

        public static void ApplyDarkToolTipStyle(FrameworkElement element)
        {
            try
            {
                Type ttType = typeof(System.Windows.Controls.ToolTip);
                Style ttStyle = new Style(ttType);
                ControlTemplate ct = new ControlTemplate(ttType);
                FrameworkElementFactory bd = new FrameworkElementFactory(typeof(Border));
                bd.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(15, 23, 42))); // Slate 900
                bd.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(51, 65, 85))); // Slate 700
                bd.SetValue(Border.BorderThicknessProperty, new Thickness(1));
                bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
                bd.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));
                bd.SetValue(Border.SnapsToDevicePixelsProperty, true);

                FrameworkElementFactory cp = new FrameworkElementFactory(typeof(ContentPresenter));
                cp.SetValue(ContentPresenter.ContentSourceProperty, "Content");
                bd.AppendChild(cp);
                ct.VisualTree = bd;

                ttStyle.Setters.Add(new Setter(Control.TemplateProperty, ct));
                ttStyle.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(241, 245, 249))));
                ttStyle.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("Segoe UI, Inter")));
                ttStyle.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));

                element.Resources[ttType] = ttStyle;
            }
            catch { }
        }
    }
}
