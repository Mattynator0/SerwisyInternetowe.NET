namespace DataGenerator;
public class SensorGenerationSettings
{
    public SensorGenerationSettings(double min, double max, double messagesPerMinute)
    {
        Min = min;
        Max = max;
        MessagesPerMinute = messagesPerMinute;
    }
    public double Min { get; set; }
    public double Max { get; set; }
    public double MessagesPerMinute { get; set; }
    public int MeasurementPublishIntervalMs =>
        MessagesPerMinute <= 0 ? 1000 : (int)(60000.0 / MessagesPerMinute);
}
