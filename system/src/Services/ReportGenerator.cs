using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WattMonitor
{
    /// <summary>
    /// Bertanggung jawab HANYA untuk merender laporan Markdown dari data SessionRecord.
    /// Output: laporan/YYYY-MM/laporan_pemakaian.md
    /// </summary>
    public static class ReportGenerator
    {
        public static string GetReportPath(DateTime date)
        {
            return Path.Combine(HistoryStore.GetMonthDir(date), "laporan_pemakaian.md");
        }

        public static void Render(List<SessionRecord> monthRecords, DateTime reportDate)
        {
            try
            {
                // Urutkan descending (terbaru di atas)
                var sorted = new List<SessionRecord>(monthRecords);
                sorted.Sort((a, b) => string.Compare(b.Date, a.Date, StringComparison.Ordinal));

                double totalMinutes = 0, totalKwh = 0, totalCost = 0;
                foreach (var r in sorted) { totalMinutes += r.Minutes; totalKwh += r.Kwh; totalCost += r.Cost; }

                string div    = "  " + new string('─', 64);
                var    output = new List<string>();

                output.Add(string.Format("  Laporan Pemakaian — {0}", reportDate.ToString("MMMM yyyy", new CultureInfo("id-ID"))));
                output.Add(div);

                double avgCost = sorted.Count > 0 ? totalCost / sorted.Count : 0.0;

                TimeSpan tsTotal = TimeSpan.FromMinutes(totalMinutes);
                output.Add(string.Format("  Durasi  : {0}:{1:D2}:{2:D2}", (int)tsTotal.TotalHours, tsTotal.Minutes, tsTotal.Seconds));
                output.Add(string.Format(CultureInfo.InvariantCulture, "  Energi  : {0:F4} kWh", Math.Ceiling(totalKwh * 10000.0) / 10000.0));
                output.Add(string.Format(CultureInfo.InvariantCulture, "  Biaya   : Rp {0:N2}", totalCost));
                output.Add(string.Format(CultureInfo.InvariantCulture, "  Harian  : Rp {0:N2}", avgCost));
                output.Add(" ");
                output.Add(" ");

                output.Add("  Catatan Harian");
                output.Add(div);
                output.Add("  Tanggal       │  Durasi   │ Pemakaian   │ Est. Biaya");
                output.Add(div);

                for (int i = 0; i < sorted.Count; i++)
                {
                    var    s   = sorted[i];
                    string pfx = (i == sorted.Count - 1) ? "  └─" : "  ├─";
                    TimeSpan ts = TimeSpan.FromMinutes(s.Minutes);
                    string dur  = string.Format("{0,2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                    double rKwh = Math.Ceiling(s.Kwh * 10000.0) / 10000.0;

                    output.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0} {1} │ {2,9} │ {3,7:F4} kWh │ Rp {4,10:N2}",
                        pfx, s.Date, dur, rKwh, s.Cost));

                    if (i == 0 && sorted.Count > 1) output.Add("  │");
                }

                string path = GetReportPath(reportDate);
                string tempPath = path + ".tmp";
                File.WriteAllLines(tempPath, output, Encoding.UTF8);
                if (File.Exists(path)) { File.Replace(tempPath, path, null); }
                else { File.Move(tempPath, path); }
            }
            catch (Exception ex) { AppLogger.Warn("ReportGenerator.Render gagal: " + ex.Message); }
        }

        public static string GetHardwareReportPath(DateTime date)
        {
            return Path.Combine(HistoryStore.GetMonthDir(date), "laporan_perangkat.md");
        }

        public static void RenderHardware(List<HardwareRecord> monthRecords, List<SessionRecord> sessionRecords, DateTime reportDate)
        {
            try
            {
                var dictByDate = new Dictionary<string, List<HardwareRecord>>();
                foreach (var r in monthRecords)
                {
                    if (!dictByDate.ContainsKey(r.Date)) dictByDate[r.Date] = new List<HardwareRecord>();
                    
                    // Merge duplicates if any
                    var existing = dictByDate[r.Date].Find(x => x.Name == r.Name);
                    if (existing != null) 
                    {
                        existing.Kwh += r.Kwh;
                        existing.Cost += r.Cost;
                    }
                    else
                    {
                        dictByDate[r.Date].Add(new HardwareRecord { Date = r.Date, Name = r.Name, Kwh = r.Kwh, Cost = r.Cost });
                    }
                }

                var sortedDates = new List<string>(dictByDate.Keys);
                sortedDates.Sort((a, b) => string.Compare(b, a, StringComparison.Ordinal)); // Descending

                string div = "  " + new string('─', 64);
                var output = new List<string>();

                output.Add(string.Format("  Laporan Perangkat — {0}", reportDate.ToString("MMMM yyyy", new CultureInfo("id-ID"))));
                output.Add(div);
                
                if (reportDate.Month == 10 && reportDate.Year == 2026)
                {
                    output.Add("  * kode baru, penghitungan banyakk miss");
                }
                
                output.Add("");
                output.Add("");

                // Calculate Monthly Totals
                var hwTotals = new Dictionary<string, HardwareRecord>();
                foreach (var r in monthRecords)
                {
                    if (!hwTotals.ContainsKey(r.Name)) hwTotals[r.Name] = new HardwareRecord { Name = r.Name, Kwh = 0, Cost = 0 };
                    hwTotals[r.Name].Kwh += r.Kwh;
                    hwTotals[r.Name].Cost += r.Cost;
                }

                var sortedTotals = new List<HardwareRecord>(hwTotals.Values);
                sortedTotals.Sort((a, b) => b.Kwh.CompareTo(a.Kwh));

                int daysCount = sortedDates.Count > 0 ? sortedDates.Count : 1;
                
                double totalMinutes = 0;
                if (sessionRecords != null)
                {
                    foreach (var sr in sessionRecords) totalMinutes += sr.Minutes;
                }
                TimeSpan tsTotal = TimeSpan.FromMinutes(totalMinutes);
                string totalDur = string.Format("{0}:{1:D2}:{2:D2}", (int)tsTotal.TotalHours, tsTotal.Minutes, tsTotal.Seconds);

                string sumDiv = "  " + new string('─', 86);
                output.Add("  Ringkasan Bulanan Perangkat");
                output.Add(sumDiv);
                output.Add("  Perangkat                  │  Durasi  │   Energi    │     Biaya     │    Harian    ");
                output.Add(sumDiv);

                foreach (var h in sortedTotals)
                {
                    double rKwh = Math.Ceiling(h.Kwh * 10000.0) / 10000.0;
                    double avgCost = h.Cost / daysCount;
                    
                    string name = h.Name;
                    if (name.Length > 26) name = name.Substring(0, 23) + "...";

                    output.Add(string.Format(CultureInfo.InvariantCulture,
                        "  {0,-26} │ {1,8} │ {2,7:F4} kWh │ Rp {3,10:N2} │ Rp {4,9:N2}",
                        name, totalDur, rKwh, h.Cost, avgCost));
                }
                output.Add("");
                output.Add("");
                
                output.Add("  Catatan Harian");
                output.Add(div);
                output.Add("");

                for (int d = 0; d < sortedDates.Count; d++)
                {
                    string date = sortedDates[d];
                    string durStr = "";
                    if (sessionRecords != null)
                    {
                        var sr = sessionRecords.Find(x => x.Date == date);
                        if (sr != null)
                        {
                            TimeSpan ts = TimeSpan.FromMinutes(sr.Minutes);
                            durStr = string.Format(" (Durasi: {0}:{1:D2}:{2:D2})", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                        }
                    }

                    output.Add("  Tanggal: " + date + durStr);
                    output.Add(div);
                    output.Add("  Perangkat                    │ Pemakaian   │ Est. Biaya");
                    output.Add(div);

                    var hwList = dictByDate[date];
                    hwList.Sort((a, b) => b.Kwh.CompareTo(a.Kwh)); // Highest KWh first

                    for (int i = 0; i < hwList.Count; i++)
                    {
                        var h = hwList[i];
                        string pfx = (i == hwList.Count - 1) ? "  └─" : "  ├─";
                        
                        string name = h.Name;
                        if (name.Length > 26) name = name.Substring(0, 23) + "...";
                        
                        double rKwh = Math.Ceiling(h.Kwh * 10000.0) / 10000.0;

                        output.Add(string.Format(CultureInfo.InvariantCulture,
                            "{0} {1,-26} │ {2,7:F4} kWh │ Rp {3,10:N2}",
                            pfx, name, rKwh, h.Cost));
                    }
                    
                    if (d < sortedDates.Count - 1)
                    {
                        output.Add(" ");
                        output.Add(" ");
                    }
                }

                string path = GetHardwareReportPath(reportDate);
                string tempPath = path + ".tmp";
                File.WriteAllLines(tempPath, output, Encoding.UTF8);
                if (File.Exists(path)) { File.Replace(tempPath, path, null); }
                else { File.Move(tempPath, path); }
            }
            catch (Exception ex) { AppLogger.Warn("ReportGenerator.RenderHardware gagal: " + ex.Message); }
        }
    }
}
