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

        public static void Load()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
                if (!File.Exists(configPath))
                {
                    string parentConfig = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config.txt"));
                    if (File.Exists(parentConfig)) configPath = parentConfig;
                }
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
    }
}
