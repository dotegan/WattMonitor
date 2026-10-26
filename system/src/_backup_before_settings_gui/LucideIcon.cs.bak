using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WattMonitor
{
    public static class LucideIcon
    {
        public const string Zap = "M13 2 3 14h9l-1 8 10-12h-9l1-8z";
        public const string Cpu = "M4 4m0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2z M9 9h6v6H9z M15 2v2 M9 2v2 M20 15h2 M20 9h2 M9 20v2 M15 20v2 M2 9h2 M2 15h2";
        public const string HardDrive = "M22 12H2 M5.45 5.11 2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z M6 16h.01 M10 16h.01";
        public const string Activity = "M22 12h-4l-3 9L9 3l-3 9H2";
        public const string Thermometer = "M14 14.76V3.5a2.5 2.5 0 0 0-5 0v11.26a4.5 4.5 0 1 0 5 0z";
        public const string Pin = "M12 17v5 M9 10.76a2 2 0 0 1-1.11 1.79l-1.78.9A2 2 0 0 0 5 15.24V16a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-.76a2 2 0 0 0-1.11-1.79l-1.78-.9A2 2 0 0 1 15 10.76V5a1 1 0 0 1 1-1 1 1 0 0 0 1-1V2H7v1a1 1 0 0 0 1 1 1 1 0 0 1 1 1z";
        public const string Lock = "M19 11H5a2 2 0 0 0-2 2v7a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2z M7 11V7a5 5 0 0 1 10 0v4";
        public const string Maximize = "M8 3H5a2 2 0 0 0-2 2v3m18 0V5a2 2 0 0 0-2-2h-3m0 18h3a2 2 0 0 0 2-2v-3M3 16v3a2 2 0 0 0 2 2h3";
        public const string Minimize = "M8 3v3a2 2 0 0 1-2 2H3m18 0h-3a2 2 0 0 1-2-2V3m0 18v-3a2 2 0 0 1 2-2h3M3 16h3a2 2 0 0 1 2 2v3";
        public const string X = "M18 6 6 18 M6 6l12 12";
        public const string BarChart = "M5 21v-6 M12 21V3 M19 21V9";
        public const string Info = "M12 2a10 10 0 1 0 10 10A10 10 0 0 0 12 2zm0 14v-4m0-4h.01";
        public const string Check = "M20 6 9 17l-5-5";
        public const string FileText = "M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z M14 2v4a2 2 0 0 0 2 2h4 M10 9H8 M16 13H8 M16 17H8";
        public const string LogOut = "M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4 M16 17l5-5-5-5 M21 12H9";
        public const string ChevronLeft = "M15 18 9 12 15 6";
        public const string ChevronRight = "M9 18 15 12 9 6";
        public const string Monitor = "M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2z M12 16v4 M8 20h8";

        public static FrameworkElement Create(string pathData, double size = 16, Brush stroke = null, bool isFilled = false, double strokeThickness = 1.8)
        {
            Viewbox vb = new Viewbox
            {
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };

            Canvas canvas = new Canvas
            {
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent
            };

            Path path = new Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = stroke ?? Brushes.White,
                StrokeThickness = isFilled ? 0 : strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round
            };

            if (isFilled)
            {
                path.Fill = stroke ?? Brushes.White;
            }

            canvas.Children.Add(path);
            vb.Child = canvas;
            return vb;
        }
    }
}
