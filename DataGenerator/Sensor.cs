using System.Text.Json;
using MQTTnet;

namespace DataGenerator;

public class Sensor(string sensorId, string type, MqttService mqttService)
{
    private string SensorId { get; set; } = sensorId;
    private string Type { get; set; } = type;

    private readonly Random _random = new();

    private double GenerateValue()
    {
        return Type switch
        {
            "light" => _random.Next(0, 1000),
            "air-quality" => _random.Next(0, 500),
            "temperature" => _random.Next(-10, 50),
            "energy" => _random.NextDouble() * 10,
            _ => 0
        };
    }

    public async Task PublishDataAsync()
    {
        var data = new
        {
            SensorId,
            Type,
            Value = GenerateValue(),
            Timestamp = DateTime.UtcNow
        };
        
        var topic = $"sensors/{Type}/{SensorId}";
        
        await mqttService.PublishSensorDataAsync(topic, data);
    }
}