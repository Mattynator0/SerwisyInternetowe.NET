namespace DataGenerator;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class SensorsController(MqttService mqttService) : ControllerBase
{
    [HttpPost("manual")]
    public async Task<IActionResult> SendManualData([FromBody] SensorData inputSensorData)
    {
        string mqttTopic = $"sensors/{inputSensorData.SensorType}";
        await mqttService.PublishSensorDataAsync(
            mqttTopic, inputSensorData);
        return Ok("Manual sensor data sent.");
    }
}

public class SensorData
{
    public string SensorId { get; set; }
    public string SensorType { get; set; }
    public double Value { get; set; }
    public long Timestamp { get; set; }
}