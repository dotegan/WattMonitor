using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(configPath)) return configPath;

            string parentConfig = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config.json"));
            if (File.Exists(parentConfig)) return parentConfig;

            // Default location: jika berjalan di dalam subfolder 'system', simpan di folder root utama
            string baseDirName = Path.GetFileName(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/'));
            if (string.Equals(baseDirName, "system", StringComparison.OrdinalIgnoreCase))
            {
                return parentConfig;
            }

            return configPath;
        }

        public static void Load()
        {
            try
            {
                string jsonPath = GetConfigPath();
                if (File.Exists(jsonPath))
                {
                    LoadFromJson(jsonPath);
                    return;
                }

                // Fallback check untuk legacy config.txt
                string txtPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
                if (!File.Exists(txtPath))
                {
                    string parentTxt = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "config.txt"));
                    if (File.Exists(parentTxt)) txtPath = parentTxt;
                }

                if (File.Exists(txtPath))
                {
                    LoadFromLegacyTxt(txtPath);
                    // Otomatis migrasi ke config.json
                    Save();
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal memuat konfigurasi: " + ex.Message, ex);
            }
        }

        private static void LoadFromJson(string filePath)
        {
            string json = File.ReadAllText(filePath, Encoding.UTF8);

            double dVal;
            int iVal;
            float fVal;

            if (TryExtractDouble(json, "Tariff", out dVal)) Tariff = dVal;
            if (TryExtractDouble(json, "HoursPerDay", out dVal)) { HoursPerDay = dVal; HoursPerMonth = dVal * 30; }
            if (TryExtractDouble(json, "HoursPerMonth", out dVal)) { HoursPerMonth = dVal; HoursPerDay = dVal / 30.0; }
            if (TryExtractDouble(json, "AutosaveMinutes", out dVal) || TryExtractDouble(json, "Autosave_Minutes", out dVal)) AutosaveMinutes = dVal;
            if (TryExtractDouble(json, "WattOffset", out dVal) || TryExtractDouble(json, "Watt_Offset", out dVal)) WattOffset = dVal;

            if (TryExtractInt(json, "RefreshRateMs", out iVal)) RefreshRateMs = Math.Max(100, iVal);
            if (TryExtractDouble(json, "Opacity", out dVal)) Opacity = Math.Max(0.1, Math.Min(1.0, dVal));
            if (TryExtractInt(json, "ConsoleWidth", out iVal)) ConsoleWidth = Math.Max(20, iVal);

            if (TryExtractFloat(json, "FontSize", out fVal) || TryExtractFloat(json, "minimalist_fontsize", out fVal)) MinimalistFontSize = fVal;
            if (TryExtractDouble(json, "MinimalistOpacity", out dVal) || TryExtractDouble(json, "minimalist_opacity", out dVal)) MinimalistOpacity = Math.Max(0.1, Math.Min(1.0, dVal));
            if (TryExtractInt(json, "Width", out iVal) || TryExtractInt(json, "minimalist_width", out iVal)) MinimalistWidth = iVal;
            if (TryExtractInt(json, "Height", out iVal) || TryExtractInt(json, "minimalist_height", out iVal)) MinimalistHeight = iVal;

            if (TryExtractDouble(json, "CpuTempOrange", out dVal) || TryExtractDouble(json, "cpu_temp_orange", out dVal)) CpuTempOrange = dVal;
            if (TryExtractDouble(json, "CpuTempRed", out dVal) || TryExtractDouble(json, "cpu_temp_red", out dVal)) CpuTempRed = dVal;
            if (TryExtractDouble(json, "GpuTempOrange", out dVal) || TryExtractDouble(json, "gpu_temp_orange", out dVal)) GpuTempOrange = dVal;
            if (TryExtractDouble(json, "GpuTempRed", out dVal) || TryExtractDouble(json, "gpu_temp_red", out dVal)) GpuTempRed = dVal;

            if (TryExtractDouble(json, "CpuWattOrange", out dVal) || TryExtractDouble(json, "cpu_watt_orange", out dVal)) CpuWattOrange = dVal;
            if (TryExtractDouble(json, "CpuWattRed", out dVal) || TryExtractDouble(json, "cpu_watt_red", out dVal)) CpuWattRed = dVal;
            if (TryExtractDouble(json, "GpuWattOrange", out dVal) || TryExtractDouble(json, "gpu_watt_orange", out dVal)) GpuWattOrange = dVal;
            if (TryExtractDouble(json, "GpuWattRed", out dVal) || TryExtractDouble(json, "gpu_watt_red", out dVal)) GpuWattRed = dVal;

            var excludes = ExtractStringArray(json, "ExcludeTexts");
            if (excludes != null && excludes.Count > 0)
            {
                ExcludeTexts = excludes;
            }

            var periphs = ExtractObjectDictionary(json, "Peripheral");
            if (periphs == null || periphs.Count == 0) periphs = ExtractObjectDictionary(json, "Peripherals");
            if (periphs != null && periphs.Count > 0)
            {
                StaticPeripherals = periphs;
            }
        }

        private static bool TryExtractDouble(string json, string key, out double value)
        {
            value = 0;
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;

            int colon = json.IndexOf(':', idx + token.Length);
            if (colon < 0) return false;

            int s = colon + 1;
            while (s < json.Length && (char.IsWhiteSpace(json[s]) || json[s] == '\"')) s++;
            int e = s;
            while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '.' || json[e] == '-')) e++;
            if (e > s)
            {
                return double.TryParse(json.Substring(s, e - s), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
            return false;
        }

        private static bool TryExtractInt(string json, string key, out int value)
        {
            value = 0;
            double d;
            if (TryExtractDouble(json, key, out d))
            {
                value = (int)d;
                return true;
            }
            return false;
        }

        private static bool TryExtractFloat(string json, string key, out float value)
        {
            value = 0f;
            double d;
            if (TryExtractDouble(json, key, out d))
            {
                value = (float)d;
                return true;
            }
            return false;
        }

        private static List<string> ExtractStringArray(string json, string key)
        {
            var list = new List<string>();
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return list;

            int openBracket = json.IndexOf('[', idx + token.Length);
            if (openBracket < 0) return list;

            int closeBracket = json.IndexOf(']', openBracket + 1);
            if (closeBracket < 0) return list;

            string arrayContent = json.Substring(openBracket + 1, closeBracket - openBracket - 1);
            int p = 0;
            while (p < arrayContent.Length)
            {
                int q1 = arrayContent.IndexOf('\"', p);
                if (q1 < 0) break;
                int q2 = arrayContent.IndexOf('\"', q1 + 1);
                if (q2 < 0) break;

                string item = arrayContent.Substring(q1 + 1, q2 - q1 - 1).Trim();
                if (!string.IsNullOrEmpty(item)) list.Add(item);
                p = q2 + 1;
            }
            return list;
        }

        private static Dictionary<string, double> ExtractObjectDictionary(string json, string key)
        {
            var dict = new Dictionary<string, double>();
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return dict;

            int openBrace = json.IndexOf('{', idx + token.Length);
            if (openBrace < 0) return dict;

            int depth = 0;
            int closeBrace = -1;
            for (int i = openBrace; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0) { closeBrace = i; break; }
                }
            }
            if (closeBrace < 0) return dict;

            string block = json.Substring(openBrace + 1, closeBrace - openBrace - 1);
            int p = 0;
            while (p < block.Length)
            {
                int q1 = block.IndexOf('\"', p);
                if (q1 < 0) break;
                int q2 = block.IndexOf('\"', q1 + 1);
                if (q2 < 0) break;

                string k = block.Substring(q1 + 1, q2 - q1 - 1).Trim();
                int colon = block.IndexOf(':', q2 + 1);
                if (colon < 0) break;

                int s = colon + 1;
                while (s < block.Length && char.IsWhiteSpace(block[s])) s++;
                int e = s;
                while (e < block.Length && (char.IsDigit(block[e]) || block[e] == '.' || block[e] == '-')) e++;
                if (e > s)
                {
                    double v;
                    if (double.TryParse(block.Substring(s, e - s), NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    {
                        dict[k] = v;
                    }
                }
                int comma = block.IndexOf(',', colon);
                if (comma < 0) break;
                p = comma + 1;
            }
            return dict;
        }

        private static void LoadFromLegacyTxt(string configPath)
        {
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

        public static void Save()
        {
            try
            {
                string path = GetConfigPath();
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"General\": {");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"Tariff\": {0:0.##},", Tariff));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"HoursPerDay\": {0:0.##},", HoursPerDay));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"AutosaveMinutes\": {0:0.##},", AutosaveMinutes));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"WattOffset\": {0:0.##}", WattOffset));
                sb.AppendLine("  },");
                sb.AppendLine("  \"Display\": {");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"RefreshRateMs\": {0},", RefreshRateMs));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"Opacity\": {0:0.##},", Opacity));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"ConsoleWidth\": {0}", ConsoleWidth));
                sb.AppendLine("  },");
                sb.AppendLine("  \"MinimalistMode\": {");
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"FontSize\": {0:0.##},", MinimalistFontSize));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"Opacity\": {0:0.##},", MinimalistOpacity));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"Width\": {0},", MinimalistWidth));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"Height\": {0},", MinimalistHeight));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"CpuTempOrange\": {0:0.##},", CpuTempOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"CpuTempRed\": {0:0.##},", CpuTempRed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"GpuTempOrange\": {0:0.##},", GpuTempOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"GpuTempRed\": {0:0.##},", GpuTempRed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"CpuWattOrange\": {0:0.##},", CpuWattOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"CpuWattRed\": {0:0.##},", CpuWattRed));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"GpuWattOrange\": {0:0.##},", GpuWattOrange));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"GpuWattRed\": {0:0.##}", GpuWattRed));
                sb.AppendLine("  },");
                sb.AppendLine("  \"Filter\": {");
                sb.AppendLine("    \"ExcludeTexts\": [");
                for (int i = 0; i < ExcludeTexts.Count; i++)
                {
                    string comma = (i < ExcludeTexts.Count - 1) ? "," : "";
                    sb.AppendLine(string.Format("      \"{0}\"{1}", ExcludeTexts[i].Replace("\"", "\\\""), comma));
                }
                sb.AppendLine("    ]");
                sb.AppendLine("  },");
                sb.AppendLine("  \"Peripheral\": {");
                int pIdx = 0;
                int pCount = StaticPeripherals.Count;
                foreach (var kvp in StaticPeripherals)
                {
                    string comma = (pIdx < pCount - 1) ? "," : "";
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "    \"{0}\": {1:0.##}{2}", kvp.Key.Replace("\"", "\\\""), kvp.Value, comma));
                    pIdx++;
                }
                sb.AppendLine("  }");
                sb.AppendLine("}");

                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal menyimpan config.json", ex);
            }
        }
    }
}
