@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

echo [WattMonitor] Compiling WPF Native Modern...
%CSC% /nologo /target:winexe /win32icon:"assets\wattmonitor.ico" /out:"WattMonitor.exe" /lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF","C:\Windows\Microsoft.NET\Framework64\v4.0.30319" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Management.dll /reference:PresentationCore.dll /reference:PresentationFramework.dll /reference:WindowsBase.dll /reference:System.Xaml.dll /reference:"libs\LibreHardwareMonitorLib.dll" /reference:"libs\HidSharp.dll" src\Core\*.cs src\Models\*.cs src\Services\*.cs src\Views\*.cs

if %ERRORLEVEL% EQU 0 (
    echo [WattMonitor] Build SUCCESS!
) else (
    echo [WattMonitor] Build FAILED!
)
