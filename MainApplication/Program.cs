using MainApplication;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MqttMongoService>(_ => new MqttMongoService(
    mqttHost: "localhost",
    mqttPort: 1883,
    mongoConnectionString: "mongodb://localhost:27017",
    mongoDbName: "sensorsDb",
    mongoCollectionName: "readings"
));

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5000/");
});
builder.Services.AddSingleton<ISensorDataService>(sp =>
{
    var mongoClient = new MongoClient("mongodb://localhost:27017");
    var database = mongoClient.GetDatabase("sensorsDb");
    var collection = database.GetCollection<SensorData>("readings");
    return new SensorDataService(collection);
});
builder.Services.AddControllers();
builder.Services.AddRazorPages();

var app = builder.Build();

var mqttService = app.Services.GetRequiredService<MqttMongoService>();
await mqttService.StartAsync();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();