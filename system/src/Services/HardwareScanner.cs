using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;

namespace WattMonitor
{
    public class HardwareSensorData
    {
        public string Name { get; set; }
        public HardwareType Type { get; set; }
        public double Watt { get; set; }
        public double? Temp { get; set; }
        public bool IsLiveSensor { get; set; }
    }

    public class HardwareUpdateResult
    {
        public double CurrentTotalWatt { get; set; }
        public List<HardwareSensorData> ActiveSensors { get; set; }
    }

    public class HardwareScanner
    {
        private Computer computer;

        public HardwareScanner()
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true,
                IsBatteryEnabled = true,
                IsPsuEnabled = true
            };
        }

        public void Open()
        {
            try
            {
                computer.Open();
            }
            catch (Exception ex)
            {
                AppLogger.Error("Gagal membuka LibreHardwareMonitor Computer: " + ex.Message, ex);
            }
        }
        
        public void Close()
        {
            try
            {
                computer.Close();
            }
            catch { }
        }
        
        public Computer GetComputer() {
            return computer;
        }

        public List<IHardware> GetHardwareList()
        {
            if (computer == null) return new List<IHardware>();
            try
            {
                return new List<IHardware>(computer.Hardware);
            }
            catch
            {
                return new List<IHardware>();
            }
        }

        public HardwareUpdateResult Update(List<HardwareComponentConfig> components)
        {
            double currentTotalWatt = 0;
            var activeSensors = new List<HardwareSensorData>();
            object _lock = new object();

            // 1. Baca sensor live dari LibreHardwareMonitor (CPU & GPU)
            foreach (var hw in computer.Hardware)
            {
                var allSensors = new List<ISensor>();
                Action<IHardware> gatherSensors = null;
                gatherSensors = (h) =>
                {
                    try { h.Update(); } catch { }
                    allSensors.AddRange(h.Sensors);
                    foreach (var sub in h.SubHardware)
                    {
                        gatherSensors(sub);
                    }
                };
                gatherSensors(hw);

                double hwWatt = 0;
                bool hasPower = false;
                double maxTemp = -100;
                bool hasTemp = false;

                // Pass 1: Identifikasi sensor power
                var powerSensors = new List<Tuple<string, double>>();
                bool hasPackage = false;
                double packageVal = 0;

                foreach (var sensor in allSensors)
                {
                    if (sensor.SensorType == SensorType.Power && sensor.Value.HasValue)
                    {
                        double val = Math.Max(0, sensor.Value.Value);
                        string sName = sensor.Name.ToLower();
                        powerSensors.Add(new Tuple<string, double>(sName, val));

                        if (sName == "package" || sName == "cpu package" || sName == "gpu package" || sName == "total")
                        {
                            hasPackage = true;
                            packageVal = Math.Max(packageVal, val);
                        }
                    }

                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    {
                        string sName = sensor.Name.ToLower();
                        if (sName.Contains("hot spot") || sName.Contains("hotspot") ||
                            sName.Contains("memory") || sName.Contains("junction"))
                            continue;

                        maxTemp = Math.Max(maxTemp, sensor.Value.Value);
                        hasTemp = true;
                    }
                }

                if (powerSensors.Count > 0)
                {
                    hasPower = true;
                    if (hasPackage)
                    {
                        hwWatt = packageVal;
                    }
                    else
                    {
                        // Fallback: jumlahkan tanpa double-count untuk CPU
                        foreach (var ps in powerSensors)
                        {
                            string sName = ps.Item1;
                            if (hw.HardwareType == HardwareType.Cpu && sName.Contains("core") && sName.Contains("#")) continue;
                            hwWatt += ps.Item2;
                        }
                    }
                }

                // Hindari double-counting VRM Motherboard
                if (hw.HardwareType == HardwareType.Motherboard || hw.HardwareType == HardwareType.SuperIO)
                {
                    hwWatt = 0;
                }

                bool hasValidTemp = hasTemp && maxTemp > 0;

                if ((hasPower || hasValidTemp) && !(hwWatt == 0 && !hasValidTemp))
                {
                    string name = Regex.Replace(hw.Name ?? "", @"\s+", " ").Trim();
                    var type = hw.HardwareType;

                    // Periksa apakah komponen ini diizinkan aktif oleh pengguna
                    bool isAllowed = true;
                    if (type == HardwareType.Cpu)
                    {
                        isAllowed = Config.IsComponentEnabled("CPU", name);
                    }
                    else if (type == HardwareType.GpuNvidia || type == HardwareType.GpuAmd || type == HardwareType.GpuIntel)
                    {
                        isAllowed = Config.IsComponentEnabled("GPU", name);
                    }
                    else if (type == HardwareType.Storage)
                    {
                        isAllowed = Config.IsComponentEnabled("Storage", name);
                    }
                    else if (type == HardwareType.Memory)
                    {
                        isAllowed = Config.IsComponentEnabled("RAM", name);
                    }
                    else if (type == HardwareType.Motherboard)
                    {
                        isAllowed = Config.IsComponentEnabled("Motherboard", name);
                    }
                    else if (type == HardwareType.Battery)
                    {
                        isAllowed = Config.IsComponentEnabled("Battery", name);
                    }

                    if (isAllowed && !string.IsNullOrEmpty(name))
                    {
                        double? temp = hasTemp ? (double?)maxTemp : null;
                        lock (_lock)
                        {
                            activeSensors.Add(new HardwareSensorData { 
                                Name = name, 
                                Type = type, 
                                Watt = hwWatt, 
                                Temp = temp,
                                IsLiveSensor = true
                            });
                            currentTotalWatt += hwWatt;
                        }
                    }
                }
            }

            // 2. Tambahkan komponen statis/estimasi yang tercentang aktif
            if (components != null)
            {
                foreach (var c in components)
                {
                    if (!c.IsLiveSensor && c.IsEnabled)
                    {
                        activeSensors.Add(new HardwareSensorData
                        {
                            Name = c.Name,
                            Type = MapCategoryToHardwareType(c.Category),
                            Watt = c.EstimatedWatt,
                            Temp = null,
                            IsLiveSensor = false
                        });
                        currentTotalWatt += c.EstimatedWatt;
                    }
                }
            }

            // Urutkan hardware untuk konsistensi tampilan (CPU, GPU, Display, Motherboard, RAM, Storage, Lainnya)
            activeSensors.Sort((a, b) =>
            {
                int orderA = GetTypeOrder(a.Type);
                int orderB = GetTypeOrder(b.Type);
                int cmp = orderA.CompareTo(orderB);
                return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            // Tambahkan watt offset kalibrasi
            currentTotalWatt += Config.WattOffset;

            return new HardwareUpdateResult
            {
                CurrentTotalWatt = currentTotalWatt,
                ActiveSensors = activeSensors
            };
        }

        private static int GetTypeOrder(HardwareType type)
        {
            switch (type)
            {
                case HardwareType.Cpu: return 1;
                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel: return 2;
                case HardwareType.Motherboard: return 3;
                case HardwareType.Memory: return 4;
                case HardwareType.Storage: return 5;
                default: return 6;
            }
        }

        private static HardwareType MapCategoryToHardwareType(string category)
        {
            if (string.IsNullOrEmpty(category)) return HardwareType.SuperIO;
            switch (category.ToLower())
            {
                case "cpu": return HardwareType.Cpu;
                case "gpu": return HardwareType.GpuNvidia;
                case "motherboard": return HardwareType.Motherboard;
                case "ram": return HardwareType.Memory;
                case "storage": return HardwareType.Storage;
                default: return HardwareType.SuperIO;
            }
        }
    }
}
