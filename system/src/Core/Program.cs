using System;
using System.Diagnostics;
using System.Windows;

namespace WattMonitor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("Failed to kill previous instance", ex);
                    }
                }
            }

            AppLogger.LogStartup();
            try
            {
                Application app = new Application();
                WattMonitorWpfWindow win = new WattMonitorWpfWindow();
                app.Run(win);
            }
            catch (Exception ex)
            {
                AppLogger.Error("Fatal unhandled WPF exception", ex);
            }
        }
    }
}
