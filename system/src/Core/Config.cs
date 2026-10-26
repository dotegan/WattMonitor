using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace WattMonitor
{
    public static class Config
    {
        public static double Tariff = 1444.70;
        public static double HoursPerDay = 10;
        public static double HoursPerMonth = 300;
        public static int RefreshRateMs = 500;
        public static int ConsoleWidth = 55;
        public static double AutosaveMinutes = 10.0;
        public static double Opacity = 0.9;
        public static double WattOffset = 0.0;
        
        public static float MinimalistFontSize = 11f;
        public static int MinimalistWidth = -1;
        public static int MinimalistHeight = -1;
        public static double MinimalistOpacity = 0.9;
        
        public static double CpuTempOrange = 55;
        public static double CpuTempRed = 68;
        public static double GpuTempOrange = 60;
        public static double GpuTempRed = 72;
        
        public static double CpuWattOrange = 45;
        public static double CpuWattRed = 60;
        public static double GpuWattOrange = 95;
        public static double GpuWattRed = 130;
        
        public static List<string> ExcludeTexts = new List<string> {
            "NVIDIA GeForce", "(TM)", "Processor", "Intel Core",
            "8-Core", "16-Core", "12-Core", "6-Core", "4-Core"
        };
        public static Dictionary<string, double> StaticPeripherals = new Dictionary<string, double>();

        public static string GetConfigPath()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            if (!File.Exists(configPath))
            {
                string parentConfig = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config.txt"));
                if (File.Exists(parentConfig)) configPath = parentConfig;
            }
            return configPath;
        }

        public static void Load()
        {
            try
            {
                string configPath = GetConfigPath();
                if (!File.Exists(configPath)) return;
                bool excludeCleared = false;
                bool inPeripheralBlock = false;

                foreach (string line in File.ReadAllLines(configPath))
                {
                    string t = line.Trim();
                    if (string.IsNullOrEmpty(t) || t.StartsWith("#") || t.StartsWith("//")) continue;

                    if (t.StartsWith("[") || t.EndsWith(":"))
                    {
                        string blockName = t.Trim('[', ']', ':');
                        inPeripheralBlock = string.Compare(blockName, "peripheral", StringComparison.OrdinalIgnoreCase) == 0;
                        continue;
                    }

                    int ci = t.IndexOf('#');
                    if (ci == -1) ci = t.IndexOf("//");
                    string actual = ci != -1 ? t.Substring(0, ci).Trim() : t;

                    int splitIdx = actual.IndexOf(':');
                    if (splitIdx == -1) splitIdx = actual.IndexOf('=');
                    if (splitIdx == -1) continue;

                    string key = actual.Substring(0, splitIdx).Trim();
                    string value = actual.Substring(splitIdx + 1).Trim();

                    if (inPeripheralBlock)
                    {
                        double pVal;
                        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pVal))
                            StaticPeripherals[key] = pVal;
                        continue;
                    }

                    double pd; int pi;
                    switch (key.ToLower())
                    {
                        case "tariff": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) Tariff = pd; break;
                        case "hoursperday": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) { HoursPerDay = pd; HoursPerMonth = pd * 30; } break;
                        case "hourspermonth": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) { HoursPerMonth = pd; HoursPerDay = pd / 30.0; } break;
                        case "refreshratems": if (int.TryParse(value, out pi)) RefreshRateMs = Math.Max(100, pi); break;
                        case "autosave_minutes": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) AutosaveMinutes = pd; break;
                        case "opacity": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) Opacity = Math.Max(0.1, Math.Min(1.0, pd)); break;
                        case "watt_offset": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) WattOffset = pd; break;
                        case "consolewidth": if (int.TryParse(value, out pi)) ConsoleWidth = Math.Max(20, pi); break;
                        case "excludetexts":
                            if (!excludeCleared) { ExcludeTexts.Clear(); excludeCleared = true; }
                            foreach (string s in value.Split(',')) if (!string.IsNullOrWhiteSpace(s)) ExcludeTexts.Add(s.Trim());
                            break;
                        case "minimalist_fontsize": float mf; if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out mf)) MinimalistFontSize = mf; break;
                        case "minimalist_width": if (int.TryParse(value, out pi)) MinimalistWidth = pi; break;
                        case "minimalist_height": if (int.TryParse(value, out pi)) MinimalistHeight = pi; break;
                        case "minimalist_opacity": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) MinimalistOpacity = pd; break;
                        case "cpu_temp_orange": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) CpuTempOrange = pd; break;
                        case "cpu_temp_red": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) CpuTempRed = pd; break;
                        case "gpu_temp_orange": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) GpuTempOrange = pd; break;
                        case "gpu_temp_red": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) GpuTempRed = pd; break;
                        case "cpu_watt_orange": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) CpuWattOrange = pd; break;
                        case "cpu_watt_red": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) CpuWattRed = pd; break;
                        case "gpu_watt_orange": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) GpuWattOrange = pd; break;
                        case "gpu_watt_red": if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out pd)) GpuWattRed = pd; break;
                    }
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                string path = GetConfigPath();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# ============================================================");
                sb.AppendLine("#  WattMonitor - config.txt");
                sb.AppendLine("#  Semua perubahan langsung berlaku setelah disimpan / restart.");
                sb.AppendLine("# ============================================================");
                sb.AppendLine();
                sb.AppendLine("General:");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Tariff:              {0:0.##}   # Tarif listrik (Rp/kWh)", Tariff));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  HoursPerDay:         {0:0.##}   # Estimasi jam aktif per hari", HoursPerDay));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Autosave_Minutes:    {0:0.##}   # Interval simpan data otomatis (menit)", AutosaveMinutes));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Watt_Offset:         {0:0.##}   # Tambahan watt tetap", WattOffset));
                sb.AppendLine();
                sb.AppendLine("Display:");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  RefreshRateMs:       {0}        # Interval update tampilan (ms)", RefreshRateMs));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  Opacity:             {0:0.##}   # Transparansi window utama (0.1 - 1.0)", Opacity));
                sb.AppendLine();
                sb.AppendLine("Minimalist Mode:");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  minimalist_fontsize: {0:0.##}   # Ukuran font mode minimalis (pt)", MinimalistFontSize));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  minimalist_opacity:  {0:0.##}   # Transparansi mode minimalis (0.1 - 1.0)", MinimalistOpacity));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  minimalist_width:    {0}        # Paksa lebar window (px, -1 otomatis)", MinimalistWidth));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  minimalist_height:   {0}        # Paksa tinggi window (px, -1 otomatis)", MinimalistHeight));
                sb.AppendLine();
                sb.AppendLine("  # Threshold Suhu & Daya (Batas perubahan warna Oranye dan Merah)");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  cpu_temp_orange:     {0:0.##}", CpuTempOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  cpu_temp_red:        {0:0.##}", CpuTempRed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  gpu_temp_orange:     {0:0.##}", GpuTempOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  gpu_temp_red:        {0:0.##}", GpuTempRed));
                sb.AppendLine();
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  cpu_watt_orange:     {0:0.##}", CpuWattOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  cpu_watt_red:        {0:0.##}", CpuWattRed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  gpu_watt_orange:     {0:0.##}", GpuWattOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  gpu_watt_red:        {0:0.##}", GpuWattRed));
                sb.AppendLine();
                sb.AppendLine("Filter:");
                sb.AppendLine("  ExcludeTexts:        " + string.Join(", ", ExcludeTexts));
                sb.AppendLine();
                sb.AppendLine("Peripheral:");
                foreach (var kvp in StaticPeripherals)
                {
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  {0}: {1:0.##}", kvp.Key, kvp.Value));
                }

                File.WriteAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal menyimpan config.txt", ex);
            }
        }
    }
}
