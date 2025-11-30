using DataGenerator;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MqttService>();
builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001);
});

var app = builder.Build();

var mqttService = app.Services.GetRequiredService<MqttService>();
await mqttService.ConnectAsync("localhost", 1883);

app.MapControllers();

List<Sensor> sensors = [];

string[] types = ["light", "air-quality", "temperature", "energy"];
foreach (var type in types)
{
    for (var j = 1; j <= 4; j++)
    {
        sensors.Add(new Sensor($"{type}{j}", type, mqttService));
    }
}

foreach (var sensor in sensors)
{
    StartSensorSending(sensor);
}

app.Run();

async void StartSensorSending(Sensor sensor)
{
    Random rnd = new();
    while (true)
    {
        await sensor.PublishDataAsync();
        await Task.Delay(rnd.Next(3000, 8000));
    }
}