namespace MainApplication
{
    public class SensorDashboardDto
    {
        public string SensorId { get; set; } = default!;
        public string Type { get; set; } = default!;
        public double LastValue { get; set; }
        public long LastTimestamp { get; set; }
        public double AverageLast100 { get; set; }
    }
}
