namespace WattMonitor
{
    public class SessionRecord
    {
        public string Date    { get; set; }
        public double Minutes { get; set; }
        public double Kwh     { get; set; }
        public double Cost    { get; set; }
    }

    public class HardwareRecord
    {
        public string Date { get; set; }
        public string Name { get; set; }
        public double Kwh { get; set; }
        public double Cost { get; set; }
    }

    public class LiveSessionSnapshot
    {
        public System.DateTime SessionStartTime { get; set; }
        public double ActiveRunningHours { get; set; }
        public double AccumulatedKwh { get; set; }
        public double UncommittedHours { get; set; }
        public double UncommittedKwh { get; set; }
        public double UncommittedCost { get; set; }
        public System.Collections.Generic.Dictionary<string, double> UncommittedHardwareKwh { get; set; }
        public double CurrentWatt { get; set; }
    }
}
