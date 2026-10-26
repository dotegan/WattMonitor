using System;
using System.IO;
using System.Text;

namespace WattMonitor
{
    public static class AppLogger
    {
        private static readonly object _lock  = new object();
        private static string _cachedLogFile  = null;
        private static int    _cachedDay      = -1;

        private static string LogFile
        {
            get
            {
                int today = DateTime.Now.Day;
                if (_cachedLogFile == null || today != _cachedDay)
                {
                    _cachedDay = today;
                    string monthFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", DateTime.Now.ToString("MM - yyyy"));
                    try { Directory.CreateDirectory(monthFolder); } catch { }
                    _cachedLogFile = Path.Combine(monthFolder, string.Format("wattmonitor_{0:yyyyMMdd}.log", DateTime.Now));
                }
                return _cachedLogFile;
            }
        }

        static AppLogger()
        {
        }

        public static void Info(string message)  { Write("INFO ", message); }
        public static void Warn(string message)  { Write("WARN ", message); }
        public static void Error(string message, Exception ex = null)
        {
            string detail = ex != null
                ? string.Format("{0} | {1}: {2}\n{3}", message, ex.GetType().Name, ex.Message, ex.StackTrace)
                : message;
            Write("ERROR", detail);
        }

        public static void LogStartup()
        {
            PurgeOldLogs();
            Info("========================================");
            Info("WattMonitor STARTED");
            Info(string.Format("OS       : {0}", Environment.OSVersion));
            Info(string.Format("CLR      : {0}", Environment.Version));
            Info(string.Format("BasePath : {0}", AppDomain.CurrentDomain.BaseDirectory));
            Info("========================================");
        }

        public static void PurgeOldLogs(int retainDays = 2)
        {
            try
            {
                string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logsDir)) return;

                DateTime cutoff = DateTime.Today.AddDays(-retainDays);

                string[] logFiles = Directory.GetFiles(logsDir, "*.log", SearchOption.AllDirectories);
                foreach (string file in logFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        bool shouldDelete = false;

                        // Check if file follows wattmonitor_yyyyMMdd pattern
                        if (fileName.StartsWith("wattmonitor_") && fileName.Length >= 20)
                        {
                            string datePart = fileName.Substring(12, 8);
                            DateTime logDate;
                            if (DateTime.TryParseExact(datePart, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out logDate))
                            {
                                if (logDate < cutoff)
                                {
                                    shouldDelete = true;
                                }
                            }
                        }
                        
                        // Fallback check based on file last write time
                        if (!shouldDelete && File.GetLastWriteTime(file) < cutoff)
                        {
                            shouldDelete = true;
                        }

                        if (shouldDelete)
                        {
                            File.Delete(file);
                        }
                    }
                    catch { }
                }

                // Clean empty month subdirectories
                string[] subDirs = Directory.GetDirectories(logsDir);
                foreach (string subDir in subDirs)
                {
                    try
                    {
                        if (Directory.GetFiles(subDir, "*", SearchOption.AllDirectories).Length == 0)
                        {
                            Directory.Delete(subDir, true);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void LogShutdown(string reason = "Normal exit")
        {
            Info(string.Format("WattMonitor SHUTDOWN | Reason: {0}", reason));
            Info("----------------------------------------");
        }

        private static void Write(string level, string message)
        {
            try
            {
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}", DateTime.Now, level, message);
                lock (_lock)
                {
                    File.AppendAllText(LogFile, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
