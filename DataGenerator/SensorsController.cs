namespace DataGenerator;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class SensorsController(MqttService mqttService) : ControllerBase
{
    [HttpPost("manual")]
    public async Task<IActionResult> SendManualData([FromBody] SensorData input)
    {
        await mqttService.PublishSensorDataAsync($"sensors/{input.Type}", input);
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