using System;
using System.Collections.Generic;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WattMonitor
{
    public static class HardwareDetector
    {
        public static List<HardwareComponentConfig> DetectAll()
        {
            var list = new List<HardwareComponentConfig>();

            bool isLaptop = false;
            try
            {
                isLaptop = (SystemInformation.PowerStatus.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery);
            }
            catch { }

            // 1. PROCESSOR (CPU)
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object val = mo["Name"];
                        if (val != null)
                        {
                            string name = Regex.Replace(val.ToString(), @"\s+", " ").Trim();
                            if (!string.IsNullOrEmpty(name))
                            {
                                list.Add(new HardwareComponentConfig(name, "CPU", true, 0.0));
                                break; // Utama saja
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal mendeteksi CPU: " + ex.Message, ex);
            }

            // Fallback CPU jika WMI gagal
            if (list.Find(c => c.Category == "CPU") == null)
            {
                list.Add(new HardwareComponentConfig("Processor (CPU)", "CPU", true, 0.0));
            }

            // 2. MOTHERBOARD
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object mVal = mo["Manufacturer"];
                        object pVal = mo["Product"];
                        string mfg = mVal != null ? mVal.ToString().Trim() : "";
                        string prod = pVal != null ? pVal.ToString().Trim() : "";
                        string mobo = (!string.IsNullOrEmpty(mfg) && prod.StartsWith(mfg, StringComparison.OrdinalIgnoreCase))
                            ? prod
                            : (mfg + " " + prod).Trim();

                        if (!string.IsNullOrEmpty(mobo))
                        {
                            double moboWatt = isLaptop ? 10.0 : 20.0;
                            list.Add(new HardwareComponentConfig(mobo, "Motherboard", false, moboWatt));
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal mendeteksi Motherboard: " + ex.Message, ex);
            }

            // 3. RAM / MEMORY
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, PartNumber, Capacity, Speed FROM Win32_PhysicalMemory"))
                {
                    int count = 0;
                    ulong totalBytes = 0;
                    string part = "";
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        count++;
                        object cVal = mo["Capacity"];
                        ulong cap = 0;
                        if (cVal != null && ulong.TryParse(cVal.ToString(), out cap)) totalBytes += cap;
                        if (string.IsNullOrEmpty(part))
                        {
                            object pVal = mo["PartNumber"];
                            if (pVal != null) part = pVal.ToString().Trim();
                        }
                    }
                    if (count > 0)
                    {
                        int totalGb = (int)Math.Round((double)totalBytes / (1024 * 1024 * 1024));
                        string ramName = string.Format("RAM {0}GB ({1}x)", totalGb, count);
                        if (!string.IsNullOrEmpty(part)) ramName += " " + part;
                        double estWatt = isLaptop ? Math.Max(2.0, count * 2.5) : Math.Max(3.0, count * 3.5);
                        list.Add(new HardwareComponentConfig(ramName, "RAM", false, estWatt));
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal mendeteksi RAM: " + ex.Message, ex);
            }

            // 4. GRAPHICS CARD (GPU)
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object nVal = mo["Name"];
                        string name = nVal != null ? nVal.ToString().Trim() : "";
                        string lower = name.ToLower();
                        if (!string.IsNullOrEmpty(name) &&
                            !lower.Contains("basic render") &&
                            !lower.Contains("rdp") &&
                            !lower.Contains("virtual") &&
                            !lower.Contains("remote"))
                        {
                            list.Add(new HardwareComponentConfig(name, "GPU", true, 0.0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal mendeteksi GPU: " + ex.Message, ex);
            }

            // 5. STORAGE / DISKS
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Model, Size, MediaType FROM Win32_DiskDrive"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object mVal = mo["Model"];
                        string model = mVal != null ? mVal.ToString().Trim() : "";
                        object sVal = mo["Size"];
                        object tVal = mo["MediaType"];
                        string type = tVal != null ? tVal.ToString().ToLower() : "";

                        if (!string.IsNullOrEmpty(model))
                        {
                            string sizeStr = "";
                            ulong bytes = 0;
                            if (sVal != null && ulong.TryParse(sVal.ToString(), out bytes))
                            {
                                double gb = bytes / (1024.0 * 1024.0 * 1024.0);
                                sizeStr = gb >= 900 ? string.Format("{0:0.#} TB", gb / 1024.0) : string.Format("{0:0} GB", gb);
                            }

                            string diskName = string.IsNullOrEmpty(sizeStr) ? model : string.Format("{0} ({1})", model, sizeStr);
                            double estWatt = type.Contains("external") || type.Contains("removable") ? 1.5 : (model.ToLower().Contains("hdd") ? 6.0 : 2.5);
                            list.Add(new HardwareComponentConfig(diskName, "Storage", false, estWatt));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal mendeteksi Storage: " + ex.Message, ex);
            }

            // 6. MONITOR / DISPLAY
            try
            {
                bool monitorFound = false;
                double defaultDisplayWatt = isLaptop ? 15.0 : 35.0;

                using (var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT UserFriendlyName FROM WmiMonitorID"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        ushort[] chars = mo["UserFriendlyName"] as ushort[];
                        if (chars != null)
                        {
                            var sb = new System.Text.StringBuilder();
                            foreach (ushort u in chars) if (u != 0) sb.Append((char)u);
                            string mName = sb.ToString().Trim();
                            if (!string.IsNullOrEmpty(mName))
                            {
                                list.Add(new HardwareComponentConfig(mName, "Display", false, defaultDisplayWatt));
                                monitorFound = true;
                            }
                        }
                    }
                }
                if (!monitorFound)
                {
                    int screenCount = Screen.AllScreens.Length;
                    for (int i = 0; i < screenCount; i++)
                    {
                        string screenName = screenCount == 1 ? (isLaptop ? "Layar Laptop" : "Monitor Utama") : string.Format("Monitor {0}", i + 1);
                        list.Add(new HardwareComponentConfig(screenName, "Display", false, defaultDisplayWatt));
                    }
                }
            }
            catch
            {
                // Fallback layar default
                list.Add(new HardwareComponentConfig(isLaptop ? "Layar Laptop" : "Monitor / Layar", "Display", false, isLaptop ? 15.0 : 35.0));
            }

            return list;
        }

        public static List<HardwareComponentConfig> MergeWithExisting(List<HardwareComponentConfig> existing, List<HardwareComponentConfig> detected)
        {
            var result = new List<HardwareComponentConfig>();
            var usedDetected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Simpan komponen dari detected, sinkronkan nilai watt/status dari existing jika sudah pernah ada
            foreach (var det in detected)
            {
                var match = existing.Find(e => 
                    string.Equals(e.Name, det.Name, StringComparison.OrdinalIgnoreCase) ||
                    (string.Equals(e.Category, det.Category, StringComparison.OrdinalIgnoreCase) && 
                     (e.Name.IndexOf(det.Name, StringComparison.OrdinalIgnoreCase) >= 0 || det.Name.IndexOf(e.Name, StringComparison.OrdinalIgnoreCase) >= 0))
                );

                if (match != null)
                {
                    det.IsEnabled = match.IsEnabled;
                    if (!det.IsLiveSensor && match.EstimatedWatt > 0)
                    {
                        det.EstimatedWatt = match.EstimatedWatt;
                    }
                    usedDetected.Add(match.Name);
                }
                result.Add(det);
            }

            // 2. Pertahankan komponen kustom (Custom) yang ditambahkan pengguna manual
            foreach (var ex in existing)
            {
                if (ex.IsCustom || (string.Equals(ex.Category, "Custom", StringComparison.OrdinalIgnoreCase) && !usedDetected.Contains(ex.Name)))
                {
                    if (result.Find(r => string.Equals(r.Name, ex.Name, StringComparison.OrdinalIgnoreCase)) == null)
                    {
                        result.Add(ex);
                    }
                }
            }

            return result;
        }
    }
}
