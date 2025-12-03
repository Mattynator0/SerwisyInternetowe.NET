using DataGenerator;

var builder = WebApplication.CreateBuilder(args);

// ----- MQTT configuration ----------------------------------------------------

string mqttHost = builder.Configuration["Mqtt:Host"] ?? "mqtt";
int mqttPort = builder.Configuration.GetValue<int?>("Mqtt:Port") ?? 1883;

// ----- DI --------------------------------------------------------------------

builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<ISensorSettingsProvider, SensorSettingsProvider>();
builder.Services.AddSingleton<SensorRunner>();

builder.Services.AddControllers();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001);
});

var app = builder.Build();

// ----- MQTT connection -------------------------------------------------------

var mqttService = app.Services.GetRequiredService<MqttService>();
await mqttService.ConnectToMqttBrokerAsync(mqttHost, mqttPort);

// ----- Sensors setup & start -------------------------------------------------

var sensorRunner = app.Services.GetRequiredService<SensorRunner>();
sensorRunner.InitializeSensors();
sensorRunner.StartPublishing();

// ----- HTTP API --------------------------------------------------------------

app.MapControllers();
app.Run();