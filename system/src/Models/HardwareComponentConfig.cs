using System;

namespace WattMonitor
{
    public class HardwareComponentConfig
    {
        public string Name { get; set; }
        public string Category { get; set; } // "CPU", "GPU", "Motherboard", "RAM", "Storage", "Display", "Custom"
        public bool IsEnabled { get; set; }
        public double EstimatedWatt { get; set; }
        public bool IsLiveSensor { get; set; }
        public bool IsCustom { get; set; }

        public HardwareComponentConfig()
        {
            IsEnabled = true;
            EstimatedWatt = 0;
            IsLiveSensor = false;
            IsCustom = false;
        }

        public HardwareComponentConfig(string name, string category, bool isLive, double defaultWatt, bool isCustom = false)
        {
            Name = name;
            Category = category;
            IsLiveSensor = isLive;
            EstimatedWatt = defaultWatt;
            IsEnabled = true;
            IsCustom = isCustom;
        }
    }
}
