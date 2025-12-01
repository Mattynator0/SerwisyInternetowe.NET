namespace DataGenerator;
public class SensorGenerationSettings
{
    public double Min { get; set; }
    public double Max { get; set; }
    public double MessagesPerMinute { get; set; }
    public int IntervalMs =>
        MessagesPerMinute <= 0 ? 1000 : (int)(60000.0 / MessagesPerMinute);
}
