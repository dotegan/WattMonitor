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
        
        // Komponen Hardware Hasil Deteksi & Estimasi
        public static List<HardwareComponentConfig> Components = new List<HardwareComponentConfig>();

        // Kompatibilitas mundur untuk peripheral statis jika dibutuhkan
        public static Dictionary<string, double> StaticPeripherals
        {
            get
            {
                var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in Components)
                {
                    if (c.IsEnabled && !c.IsLiveSensor)
                    {
                        dict[c.Name] = c.EstimatedWatt;
                    }
                }
                return dict;
            }
        }

        public static bool IsComponentEnabled(string category, string name = null)
        {
            if (Components == null || Components.Count == 0) return true;

            foreach (var c in Components)
            {
                if (string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) ||
                            name.IndexOf(c.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            c.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return c.IsEnabled;
                        }
                    }
                    else
                    {
                        return c.IsEnabled;
                    }
                }
            }
            return true;
        }

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
                    Save();
                }
                else
                {
                    // Inisialisasi awal jika belum ada file konfigurasi
                    Components = HardwareDetector.DetectAll();
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

            // Memuat daftar Komponen Terdeteksi & Estimasi secara Portable
            var parsedComponents = ExtractComponentsArray(json, "Components");
            var detected = HardwareDetector.DetectAll();

            if (parsedComponents != null && parsedComponents.Count > 0)
            {
                // Cek apakah PC saat ini sama dengan profil komponen yang tersimpan di config
                var detectedCpu = detected.Find(d => d.Category == "CPU");
                var savedCpu = parsedComponents.Find(p => p.Category == "CPU");
                bool isSamePc = (detectedCpu != null && savedCpu != null && 
                    (string.Equals(detectedCpu.Name, savedCpu.Name, StringComparison.OrdinalIgnoreCase) ||
                     detectedCpu.Name.IndexOf(savedCpu.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     savedCpu.Name.IndexOf(detectedCpu.Name, StringComparison.OrdinalIgnoreCase) >= 0));

                if (isSamePc)
                {
                    // PC yang sama: pertahankan toggle aktif/nonaktif dan kustomisasi user
                    Components = HardwareDetector.MergeWithExisting(parsedComponents, detected);
                }
                else
                {
                    // PORTABLE: Berpindah ke PC/Laptop baru! Otomatis gunakan spesifikasi PC baru
                    AppLogger.Info("Terdeteksi berpindah ke PC baru (" + (detectedCpu != null ? detectedCpu.Name : "Unknown") + "). Mengadaptasi hardware baru...");
                    Components = detected;
                    Save();
                }
            }
            else
            {
                // Jika file config belum punya array Components:
                var legacyPeriphs = ExtractObjectDictionary(json, "Peripheral");
                if (legacyPeriphs == null || legacyPeriphs.Count == 0) legacyPeriphs = ExtractObjectDictionary(json, "Peripherals");

                if (legacyPeriphs != null && legacyPeriphs.Count > 0)
                {
                    var matchedLegacy = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var det in detected)
                    {
                        foreach (var kvp in legacyPeriphs)
                        {
                            if (det.Name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                kvp.Key.IndexOf(det.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (!det.IsLiveSensor) det.EstimatedWatt = kvp.Value;
                                matchedLegacy.Add(kvp.Key);
                                break;
                            }
                        }
                    }

                    // Tambahkan sisa peripheral manual lama yang belum masuk deteksi
                    foreach (var kvp in legacyPeriphs)
                    {
                        if (!matchedLegacy.Contains(kvp.Key))
                        {
                            detected.Add(new HardwareComponentConfig(kvp.Key, "Custom", false, kvp.Value, true));
                        }
                    }
                }

                Components = detected;
                Save(); // Simpan format baru
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

        private static bool TryExtractBool(string json, string key, out bool value)
        {
            value = true;
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return false;

            int colon = json.IndexOf(':', idx + token.Length);
            if (colon < 0) return false;

            int s = colon + 1;
            while (s < json.Length && char.IsWhiteSpace(json[s])) s++;
            if (s < json.Length)
            {
                string rest = json.Substring(s);
                if (rest.StartsWith("true", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
                if (rest.StartsWith("false", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
            }
            return false;
        }

        private static string ExtractStringValue(string json, string key)
        {
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return "";

            int colon = json.IndexOf(':', idx + token.Length);
            if (colon < 0) return "";

            int q1 = json.IndexOf('\"', colon + 1);
            if (q1 < 0) return "";

            int q2 = json.IndexOf('\"', q1 + 1);
            if (q2 < 0) return "";

            return json.Substring(q1 + 1, q2 - q1 - 1).Trim();
        }

        private static List<HardwareComponentConfig> ExtractComponentsArray(string json, string key)
        {
            var list = new List<HardwareComponentConfig>();
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return list;

            int openBracket = json.IndexOf('[', idx + token.Length);
            if (openBracket < 0) return list;

            int closeBracket = -1;
            int depth = 0;
            for (int i = openBracket; i < json.Length; i++)
            {
                if (json[i] == '[') depth++;
                else if (json[i] == ']')
                {
                    depth--;
                    if (depth == 0) { closeBracket = i; break; }
                }
            }
            if (closeBracket < 0) return list;

            string content = json.Substring(openBracket + 1, closeBracket - openBracket - 1);
            int p = 0;
            while (p < content.Length)
            {
                int ob = content.IndexOf('{', p);
                if (ob < 0) break;
                int cb = -1;
                int bDepth = 0;
                for (int i = ob; i < content.Length; i++)
                {
                    if (content[i] == '{') bDepth++;
                    else if (content[i] == '}')
                    {
                        bDepth--;
                        if (bDepth == 0) { cb = i; break; }
                    }
                }
                if (cb < 0) break;

                string itemJson = content.Substring(ob, cb - ob + 1);
                string name = ExtractStringValue(itemJson, "Name");
                string cat = ExtractStringValue(itemJson, "Category");
                bool enabled = true;
                TryExtractBool(itemJson, "IsEnabled", out enabled);
                double watt = 0;
                if (!TryExtractDouble(itemJson, "Watt", out watt))
                {
                    TryExtractDouble(itemJson, "EstimatedWatt", out watt);
                }
                bool isLive = false;
                if (!TryExtractBool(itemJson, "IsLive", out isLive))
                {
                    TryExtractBool(itemJson, "IsLiveSensor", out isLive);
                }
                bool isCustom = false;
                TryExtractBool(itemJson, "IsCustom", out isCustom);

                if (!string.IsNullOrEmpty(name))
                {
                    list.Add(new HardwareComponentConfig
                    {
                        Name = name,
                        Category = string.IsNullOrEmpty(cat) ? "Custom" : cat,
                        IsEnabled = enabled,
                        EstimatedWatt = watt,
                        IsLiveSensor = isLive,
                        IsCustom = isCustom
                    });
                }

                p = cb + 1;
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
            var periphs = new Dictionary<string, double>();
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
                        periphs[key] = pVal;
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

            var detected = HardwareDetector.DetectAll();
            foreach (var kvp in periphs)
            {
                var match = detected.Find(d => d.Name.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null && !match.IsLiveSensor)
                {
                    match.EstimatedWatt = kvp.Value;
                }
                else
                {
                    detected.Add(new HardwareComponentConfig(kvp.Key, "Custom", false, kvp.Value, true));
                }
            }
            Components = detected;
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
                sb.AppendLine("  \"Components\": [");
                for (int i = 0; i < Components.Count; i++)
                {
                    var c = Components[i];
                    string comma = (i < Components.Count - 1) ? "," : "";
                    sb.AppendLine("    {");
                    sb.AppendLine(string.Format("      \"Name\": \"{0}\",", (c.Name ?? "").Replace("\"", "\\\"")));
                    sb.AppendLine(string.Format("      \"Category\": \"{0}\",", (c.Category ?? "").Replace("\"", "\\\"")));
                    sb.AppendLine(string.Format("      \"IsEnabled\": {0},", c.IsEnabled ? "true" : "false"));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "      \"Watt\": {0:0.##},", c.EstimatedWatt));
                    sb.AppendLine(string.Format("      \"IsLive\": {0},", c.IsLiveSensor ? "true" : "false"));
                    sb.AppendLine(string.Format("      \"IsCustom\": {0}", c.IsCustom ? "true" : "false"));
                    sb.AppendLine("    }" + comma);
                }
                sb.AppendLine("  ]");
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
