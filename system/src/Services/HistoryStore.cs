using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WattMonitor
{
    /// <summary>
    /// Bertanggung jawab HANYA untuk membaca/menulis data riwayat per bulan.
    /// Path: laporan/YYYY-MM/history.json
    /// Setiap bulan memiliki file sendiri — tidak ada akumulasi antar bulan dalam 1 file.
    /// </summary>
    public static class HistoryStore
    {
        public static string GetMonthDir(DateTime date)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "laporan", date.ToString("yyyy-MM"));
            try { Directory.CreateDirectory(dir); } catch { }
            return dir;
        }

        private static string GetPath(DateTime date)
        {
            return Path.Combine(GetMonthDir(date), "history.json");
        }

        public static List<SessionRecord> Load(DateTime date)
        {
            var result = new List<SessionRecord>();
            string path = GetPath(date);
            if (!File.Exists(path)) return result;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8).Trim();
                if (!json.StartsWith("[")) return result;

                int depth = 0, start = -1;
                for (int i = 0; i < json.Length; i++)
                {
                    if (json[i] == '{') { if (depth++ == 0) start = i; }
                    else if (json[i] == '}' && --depth == 0 && start >= 0)
                    {
                        var rec = ParseRecord(json.Substring(start, i - start + 1));
                        if (rec != null) result.Add(rec);
                        start = -1;
                    }
                }
            }
            catch (Exception ex) { AppLogger.Warn("HistoryStore.Load gagal: " + ex.Message); }
            return result;
        }

        public static void Save(List<SessionRecord> records, DateTime date)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    sb.AppendFormat(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "  {{ \"date\": \"{0}\", \"minutes\": {1:F4}, \"kwh\": {2:F6}, \"cost\": {3:F4} }}{4}",
                        r.Date, r.Minutes, r.Kwh, r.Cost,
                        i < records.Count - 1 ? "," : "");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                string path = GetPath(date);
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, sb.ToString(), Encoding.UTF8);
                if (File.Exists(path)) { File.Replace(tempPath, path, null); }
                else { File.Move(tempPath, path); }
            }
            catch (Exception ex) { AppLogger.Warn("HistoryStore.Save gagal: " + ex.Message); }
        }

        private static SessionRecord ParseRecord(string obj)
        {
            try
            {
                var r = new SessionRecord
                {
                    Date    = ExtractString(obj, "date"),
                    Minutes = ExtractDouble(obj, "minutes"),
                    Kwh     = ExtractDouble(obj, "kwh"),
                    Cost    = ExtractDouble(obj, "cost")
                };
                return string.IsNullOrEmpty(r.Date) ? null : r;
            }
            catch { return null; }
        }

        private static string ExtractString(string json, string key)
        {
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx + token.Length);
            int q1 = json.IndexOf('"', colon + 1);
            int q2 = json.IndexOf('"', q1 + 1);
            return (q1 >= 0 && q2 > q1) ? json.Substring(q1 + 1, q2 - q1 - 1) : null;
        }

        private static double ExtractDouble(string json, string key)
        {
            string token = "\"" + key + "\"";
            int idx = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return 0;
            int colon = json.IndexOf(':', idx + token.Length);
            int s = colon + 1;
            while (s < json.Length && (json[s] == ' ' || json[s] == '\t')) s++;
            int e = s;
            while (e < json.Length && (char.IsDigit(json[e]) || json[e] == '.' || json[e] == '-')) e++;
            double val;
            double.TryParse(json.Substring(s, e - s),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out val);
            return val;
        }

        private static string GetHardwarePath(DateTime date)
        {
            return Path.Combine(GetMonthDir(date), "history_hardware.json");
        }

        public static List<HardwareRecord> LoadHardware(DateTime date)
        {
            var result = new List<HardwareRecord>();
            string path = GetHardwarePath(date);
            if (!File.Exists(path)) return result;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8).Trim();
                if (!json.StartsWith("[")) return result;

                int depth = 0, start = -1;
                for (int i = 0; i < json.Length; i++)
                {
                    if (json[i] == '{') { if (depth++ == 0) start = i; }
                    else if (json[i] == '}' && --depth == 0 && start >= 0)
                    {
                        var recStr = json.Substring(start, i - start + 1);
                        var rec = new HardwareRecord
                        {
                            Date = ExtractString(recStr, "date"),
                            Name = ExtractString(recStr, "name"),
                            Kwh  = ExtractDouble(recStr, "kwh"),
                            Cost = ExtractDouble(recStr, "cost")
                        };
                        if (!string.IsNullOrEmpty(rec.Date) && !string.IsNullOrEmpty(rec.Name)) result.Add(rec);
                        start = -1;
                    }
                }
            }
            catch (Exception ex) { AppLogger.Warn("HistoryStore.LoadHardware gagal: " + ex.Message); }
            return result;
        }

        public static void SaveHardware(List<HardwareRecord> records, DateTime date)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    sb.AppendFormat(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "  {{ \"date\": \"{0}\", \"name\": \"{1}\", \"kwh\": {2:F6}, \"cost\": {3:F4} }}{4}",
                        r.Date, r.Name, r.Kwh, r.Cost,
                        i < records.Count - 1 ? "," : "");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                string path = GetHardwarePath(date);
                string tempPath = path + ".tmp";
                File.WriteAllText(tempPath, sb.ToString(), Encoding.UTF8);
                if (File.Exists(path)) { File.Replace(tempPath, path, null); }
                else { File.Move(tempPath, path); }
            }
            catch (Exception ex) { AppLogger.Warn("HistoryStore.SaveHardware gagal: " + ex.Message); }
        }
    }
}
