using MainApplication;
using MainApplication.Blockchain;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

var mqttHost = builder.Configuration["Mqtt:Host"] ?? "mqtt";
var mqttPort = builder.Configuration.GetValue<int?>("Mqtt:Port") ?? 1883;

var mongoConnectionString = builder.Configuration["Mongo:ConnectionString"] ?? "mongodb://mongo:27017";
var mongoDbName = builder.Configuration["Mongo:Database"] ?? "sensorsDb";
var mongoCollectionName = builder.Configuration["Mongo:Collection"] ?? "readings";

builder.Services.AddSingleton<IBlockchainRewardService, BlockchainRewardService>();
builder.Services.AddSingleton<MqttMongoService>(sp =>
{
    var rewardService = sp.GetRequiredService<IBlockchainRewardService>();
    return new MqttMongoService(
        mqttHost,
        mqttPort,
        mongoConnectionString,
        mongoDbName,
        mongoCollectionName,
        rewardService);
});
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