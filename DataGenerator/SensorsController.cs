namespace DataGenerator;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class SensorsController(MqttService mqttService) : ControllerBase
{
    [HttpPost("manual")]
    public async Task<IActionResult> SendManualData([FromBody] SensorData inputSensorData)
    {
        string mqttTopic = $"sensors/{inputSensorData.Type}";
        if (inputSensorData.Timestamp == 0)
            inputSensorData.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await mqttService.PublishSensorDataAsync(
            mqttTopic, inputSensorData);
        return Ok("Manual sensor data sent.");
    }
}

public class SensorData
{
    public string SensorId { get; set; }
    public string Type { get; set; }
    public double Value { get; set; }
    public long Timestamp { get; set; }
}