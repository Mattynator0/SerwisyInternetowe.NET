using MainApplication;

var builder = WebApplication.CreateBuilder(args);

// ----- application services --------------------------------------------------

builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddRazorPages();

// host on port 5000 for Docker
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// ----- start MQTT subscription ----------------------------------------------

var mqttService = app.Services.GetRequiredService<MqttMongoService>();
await mqttService.StartAsync();

// ----- HTTP pipeline ---------------------------------------------------------

app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();