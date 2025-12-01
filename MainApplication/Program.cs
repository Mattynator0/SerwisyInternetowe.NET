using MainApplication;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var mqttHost = builder.Configuration["Mqtt:Host"] ?? "mqtt";
var mqttPort = builder.Configuration.GetValue<int?>("Mqtt:Port") ?? 1883;

var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://mongo:27017";
var mongoDbName = builder.Configuration["Mongo:Database"] ?? "sensorsDb";
var mongoCollectionName = builder.Configuration["Mongo:Collection"] ?? "readings";

builder.Services.AddSingleton<MqttMongoService>(_ => new MqttMongoService(
    mqttHost: mqttHost,
    mqttPort: mqttPort,
    mongoConnectionString: mongoConnectionString,
    mongoDbName: mongoDbName,
    mongoCollectionName: mongoCollectionName
));

builder.Services.AddHttpClient("ApiClient", client =>
{
    var baseUrl = builder.Configuration["Backend:BaseUrl"] ?? "http://localhost:5000/";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddSingleton<ISensorDataService>(sp =>
{
    var mongoClient = new MongoClient(mongoConnectionString);
    var database = mongoClient.GetDatabase(mongoDbName);
    var collection = database.GetCollection<SensorData>(mongoCollectionName);
    return new SensorDataService(collection);
});

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

var mqttService = app.Services.GetRequiredService<MqttMongoService>();
await mqttService.StartAsync();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();

app.Run();
app.Run();