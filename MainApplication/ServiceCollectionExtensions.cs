using MainApplication.Blockchain;
using MongoDB.Driver;

namespace MainApplication;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mqttSettings = ReadMqttSettings(configuration);
        var mongoSettings = ReadMongoSettings(configuration);
        var backendBaseUrl = ReadBackendBaseUrl(configuration);

        RegisterBlockchain(services);
        RegisterMqttMongoService(services, mqttSettings, mongoSettings);
        RegisterHttpClient(services, backendBaseUrl);
        RegisterSensorDataService(services, mongoSettings);
    }

    // ---------- settings readers ----------

    private static MqttSettings ReadMqttSettings(IConfiguration configuration)
    {
        var host = configuration["Mqtt:Host"] ?? "mqtt";
        var port = configuration.GetValue<int?>("Mqtt:Port") ?? 1883;

        return new MqttSettings(host, port);
    }

    private static MongoSettings ReadMongoSettings(IConfiguration configuration)
    {
        var connectionString =
            configuration["Mongo:ConnectionString"] ?? "mongodb://mongo:27017";
        var databaseName =
            configuration["Mongo:Database"] ?? "sensorsDb";
        var collectionName =
            configuration["Mongo:Collection"] ?? "readings";

        return new MongoSettings(connectionString, databaseName, collectionName);
    }

    private static string ReadBackendBaseUrl(IConfiguration configuration)
    {
        return configuration["Backend:BaseUrl"] ?? "http://localhost:5000/";
    }

    // ---------- registrations ----------

    private static void RegisterBlockchain(IServiceCollection services)
    {
        services.AddSingleton<IBlockchainRewardService, BlockchainRewardService>();
    }

    private static void RegisterMqttMongoService(
        IServiceCollection services,
        MqttSettings mqtt,
        MongoSettings mongo)
    {
        services.AddSingleton<MqttMongoService>(sp =>
        {
            var rewardService = sp.GetRequiredService<IBlockchainRewardService>();

            return new MqttMongoService(
                mqttHost: mqtt.Host,
                mqttPort: mqtt.Port,
                mongoConnectionString: mongo.ConnectionString,
                mongoDbName: mongo.DatabaseName,
                mongoCollectionName: mongo.CollectionName,
                blockchainRewardService: rewardService);
        });
    }

    private static void RegisterHttpClient(IServiceCollection services, string backendBaseUrl)
    {
        services.AddHttpClient("ApiClient", client =>
        {
            client.BaseAddress = new Uri(backendBaseUrl);
        });
    }

    private static void RegisterSensorDataService(
        IServiceCollection services,
        MongoSettings mongo)
    {
        services.AddSingleton<ISensorDataService>(sp =>
        {
            var mongoClient = new MongoClient(mongo.ConnectionString);
            var database = mongoClient.GetDatabase(mongo.DatabaseName);
            var collection = database.GetCollection<SensorData>(mongo.CollectionName);

            return new SensorDataService(collection);
        });
    }

    // ---------- small settings records ----------

    private sealed record MqttSettings(string Host, int Port);

    private sealed record MongoSettings(
        string ConnectionString,
        string DatabaseName,
        string CollectionName);
}
