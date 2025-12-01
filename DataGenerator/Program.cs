using DataGenerator;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

var mqttHost = builder.Configuration["Mqtt:Host"] ?? "mqtt";
var mqttPort = builder.Configuration.GetValue<int?>("Mqtt:Port") ?? 1883;

SensorGenerationSettings GetSettings(string type, double defMin, double defMax, double defMessagesPerMinute)
{
    var section = builder.Configuration.GetSection($"Sensors:{type}");
    var min = section.GetValue<double?>("Min") ?? defMin;
    var max = section.GetValue<double?>("Max") ?? defMax;
    var mpm = section.GetValue<double?>("MessagesPerMinute") ?? defMessagesPerMinute;

    return new SensorGenerationSettings
    {
        Min = min,
        Max = max,
        MessagesPerMinute = mpm
    };
}

var sensorSettingsByType = new Dictionary<string, SensorGenerationSettings>
{
    ["light"] = GetSettings("light", 0, 1000, 20),
    ["air-quality"] = GetSettings("air-quality", 0, 500, 15),
    ["temperature"] = GetSettings("temperature", -10, 50, 30),
    ["energy"] = GetSettings("energy", 0, 10, 10),
};

builder.Services.AddSingleton<MqttService>();
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001);
});

var app = builder.Build();

var mqttService = app.Services.GetRequiredService<MqttService>();
await mqttService.ConnectAsync(mqttHost, mqttPort);

app.MapControllers();

List<Sensor> sensors = new();

string[] types = new[] { "light", "air-quality", "temperature", "energy" };
foreach (var type in types)
{
    var settings = sensorSettingsByType[type];
    for (var j = 1; j <= 4; j++)
    {
        sensors.Add(new Sensor(
            sensorId: $"{type}{j}",
            type: type,
            mqttService: mqttService,
            min: settings.Min,
            max: settings.Max,
            intervalMs: settings.IntervalMs));
    }
}

foreach (var sensor in sensors)
{
    StartSensorSending(sensor);
}

app.Run();

async void StartSensorSending(Sensor sensor)
{
    while (true)
    {
        await sensor.PublishDataAsync();
        await Task.Delay(sensor.IntervalMs);
    }
}
