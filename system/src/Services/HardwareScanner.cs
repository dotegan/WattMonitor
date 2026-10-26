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
                IsStorageEnabled = false,
                IsNetworkEnabled = false
            };
        }

        public void Open()
        {
            computer.Open();
        }
        
        public void Close()
        {
            computer.Close();
        }
        
        public Computer GetComputer() {
            return computer;
        }

        public HardwareUpdateResult Update(List<string> excludeTexts, Dictionary<string, double> staticPeripherals)
        {
            double currentTotalWatt = 0;
            var activeSensors = new List<HardwareSensorData>();
            object _lock = new object();

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

                // Pass 1: Identify power sensors
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
                            packageVal = Math.Max(packageVal, val); // in case there are multiple, take highest
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
                        // Fallback: sum them up, but carefully avoid double counting for CPUs
                        foreach (var ps in powerSensors)
                        {
                            string sName = ps.Item1;
                            if (hw.HardwareType == HardwareType.Cpu && sName.Contains("core") && sName.Contains("#")) continue;
                            hwWatt += ps.Item2;
                        }
                    }
                }

                // Prevent VRM double-counting (Motherboard reports what goes into the CPU)
                if (hw.HardwareType == HardwareType.Motherboard || hw.HardwareType == HardwareType.SuperIO)
                {
                    hwWatt = 0;
                }

                bool hasValidTemp = hasTemp && maxTemp > 0;

                if ((hasPower || hasValidTemp) && !(hwWatt == 0 && !hasValidTemp))
                {
                    string name = hw.Name;
                    var type = hw.HardwareType;

                    if (type == HardwareType.GpuAmd && name.ToLower().Contains("radeon") && name.ToLower().Contains("graphics"))
                    {
                        type = HardwareType.Cpu;
                        name = name.Replace("Radeon(TM) Graphics", "Ryzen (iGPU)").Replace("Radeon Graphics", "Ryzen (iGPU)");
                    }

                    foreach (string rm in excludeTexts)
                        name = Regex.Replace(name, Regex.Escape(rm), "", RegexOptions.IgnoreCase);
                    name = Regex.Replace(name, @"\s+", " ").Trim();

                    if (!string.IsNullOrEmpty(name))
                    {
                        double? temp = hasTemp ? (double?)maxTemp : null;
                        lock (_lock)
                        {
                            activeSensors.Add(new HardwareSensorData { 
                                Name = name, 
                                Type = type, 
                                Watt = hwWatt, 
                                Temp = temp
                            });
                            currentTotalWatt += hwWatt;
                        }
                    }
                }
            }

            // Urutkan hardware untuk konsistensi tampilan
            activeSensors.Sort((a, b) =>
            {
                int typeCompare = a.Type.CompareTo(b.Type);
                return typeCompare != 0 ? typeCompare : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });

            // Tambahkan konsumsi peripheral statis
            double peripheralWatt = 0;
            foreach (var kv in staticPeripherals) peripheralWatt += kv.Value;
            currentTotalWatt += peripheralWatt;
            currentTotalWatt += Config.WattOffset;

            return new HardwareUpdateResult
            {
                CurrentTotalWatt = currentTotalWatt,
                ActiveSensors = activeSensors
            };
        }
    }
}
