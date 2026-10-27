using System;
using System.Diagnostics;
using System.IO;

class Launcher
{
    [STAThread]
    static void Main()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string appExe = Path.Combine(baseDir, "system", "WattMonitor.exe");
            if (!File.Exists(appExe)) return;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = appExe,
                WorkingDirectory = Path.Combine(baseDir, "system"),
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch { }
    }
}
