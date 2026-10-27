using System;
using System.Collections.Generic;

namespace WattMonitor
{
    /// <summary>
    /// Koordinator sesi. Hanya mengelola data bulan berjalan.
    /// Setiap bulan: data dimuat dari dan disimpan ke folder laporan/YYYY-MM/ masing-masing.
    /// </summary>
    public static class SessionManager
    {
        // Hanya menyimpan record bulan yang sedang berjalan
        public static List<SessionRecord> HistoricalSessions = new List<SessionRecord>();
        public static List<HardwareRecord> HistoricalHardware = new List<HardwareRecord>();

        public static void LoadHistoricalLogs(DateTime date)
        {
            HistoricalSessions = HistoryStore.Load(date);
            HistoricalHardware = HistoryStore.LoadHardware(date);
        }

        public static void CommitCurrentSession(DateTime sessionDate, double sessionMins, double sessionKwh, double sessionCost, Dictionary<string, double> hardwareKwh)
        {
            string curDate  = sessionDate.ToString("yyyy-MM-dd");
            var    existing = HistoricalSessions.Find(s => s.Date == curDate);

            if (existing != null)
            {
                existing.Minutes += sessionMins;
                existing.Kwh     += sessionKwh;
                existing.Cost    += sessionCost;
            }
            else
            {
                HistoricalSessions.Add(new SessionRecord
                {
                    Date    = curDate,
                    Minutes = sessionMins,
                    Kwh     = sessionKwh,
                    Cost    = sessionCost
                });
            }

            if (hardwareKwh != null)
            {
                foreach (var kvp in hardwareKwh)
                {
                    var existHw = HistoricalHardware.Find(h => h.Date == curDate && h.Name == kvp.Key);
                    double cost = kvp.Value * Config.Tariff;
                    if (existHw != null)
                    {
                        existHw.Kwh += kvp.Value;
                        existHw.Cost += cost;
                    }
                    else
                    {
                        HistoricalHardware.Add(new HardwareRecord
                        {
                            Date = curDate,
                            Name = kvp.Key,
                            Kwh = kvp.Value,
                            Cost = cost
                        });
                    }
                }
            }
        }

        public static void WriteSessionLog(DateTime sessionStartTime, double sessionMins, double sessionKwh, double sessionCost, Dictionary<string, double> hardwareKwh)
        {
            CommitCurrentSession(sessionStartTime, sessionMins, sessionKwh, sessionCost, hardwareKwh);
            HistoryStore.Save(HistoricalSessions, sessionStartTime);
            HistoryStore.SaveHardware(HistoricalHardware, sessionStartTime);
        }
    }
}
