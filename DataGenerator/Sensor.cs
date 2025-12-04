namespace DataGenerator;

public class Sensor
{
    private string SensorId { get; }
    private string SensorType { get; }
    private readonly MqttService _mqttService;

    private readonly double _min;
    private readonly double _max;

    private readonly Random _random = new();

    public int MeasurementPublishIntervalMs { get; }

    public Sensor(
        string sensorId, 
        string sensorType, 
        MqttService mqttService,
        double min, 
        double max,
        int measurementPublishIntervalMs)
    {
        SensorId = sensorId;
        SensorType = sensorType;
        _mqttService = mqttService;
        _min = min;
        _max = max;
        MeasurementPublishIntervalMs = measurementPublishIntervalMs;
    }

    private double GenerateRandomValueInRange()
    {
        double range = _max - _min;
        if (range <= 0)
            return _min;

        return _min + _random.NextDouble() * range;
    }

    public async Task PublishMeasurementDataAsync()
    {
        var measurement = new
        {
            SensorId,
            Type = SensorType,
            Value = GenerateRandomValueInRange(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        string topic = $"sensors/{SensorType}/{SensorId}";
        await _mqttService.PublishSensorDataAsync(topic, measurement);
    }
}