namespace DataGenerator;

public class Sensor
{
    private string SensorId { get; }
    private string Type { get; }
    private readonly MqttService _mqttService;

    private readonly double _min;
    private readonly double _max;

    private readonly Random _random = new();

    public int IntervalMs { get; }

    public Sensor(string sensorId, string type, MqttService mqttService,
                  double min, double max, int intervalMs)
    {
        SensorId = sensorId;
        Type = type;
        _mqttService = mqttService;
        _min = min;
        _max = max;
        IntervalMs = intervalMs;
    }

    private double GenerateValue()
    {
        var range = _max - _min;
        if (range <= 0)
            return _min;

        return _min + _random.NextDouble() * range;
    }

    public async Task PublishDataAsync()
    {
        var data = new
        {
            SensorId,
            Type,
            Value = GenerateValue(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var topic = $"sensors/{Type}/{SensorId}";
        await _mqttService.PublishSensorDataAsync(topic, data);
    }
}